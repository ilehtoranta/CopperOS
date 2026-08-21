/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

public static class MuiHeadlessDispatcher
{
	// Private semantic IDs only; public ABI declarations remain SDK-owned.
	private const uint Notify = 0x8042C9CB;
	private const uint CallHook = 0x8042B96B;
	private const uint GetConfigItem = 0x80423EDB;
	private const uint KillNotify = 0x8042D240;
	private const uint KillNotifyObj = 0x8042B145;
	private const uint MultiSet = 0x8042D356;
	private const uint FindObject = 0x8042038F;
	private const uint Set = 0x8042549A;
	private const uint NoNotifySet = 0x8042216F;
	private const uint WriteLong = 0x80428D86;
	private const uint WriteString = 0x80424BF4;
	private const uint SetAsString = 0x80422590;
	private const uint Export = MuiObjectPersistenceMessageCore.ExportMethod;
	private const uint Import = MuiObjectPersistenceMessageCore.ImportMethod;
	private const uint FamilyAddHead = MuiFamilyMutationCore.AddHeadMethod;
	private const uint FamilyAddTail = MuiFamilyMutationCore.AddTailMethod;
	private const uint FamilyDoChildMethods = 0x80429A3C;
	private const uint FamilyGetChild = 0x8042C556;
	private const uint FamilyInsert = MuiFamilyMutationCore.InsertMethod;
	private const uint FamilyRemove = MuiFamilyMutationCore.RemoveMethod;
	private const uint FamilyReorder = MuiFamilyMutationCore.ReorderMethod;
	private const uint FamilySort = MuiFamilyMutationCore.SortMethod;
	private const uint FamilyTransfer = MuiFamilyMutationCore.TransferMethod;
	private const uint GroupInitChange = MuiGroupChangeCore.InitChangeMethod;
	private const uint GroupExitChange = MuiGroupChangeCore.ExitChangeMethod;
	private const uint GroupExitChange2 = MuiGroupChangeCore.ExitChange2Method;
	private const uint GroupMoveMember = 0x8042FF4E;
	private const uint GroupReorder = 0x80426C3F;
	private const uint GroupSort = 0x80427417;
	private const uint DataspaceAdd = 0x80423366;
	private const uint DataspaceClear = 0x8042B6C9;
	private const uint DataspaceFind = 0x8042832C;
	private const uint DataspaceGet = 0x8042483F;
	private const uint DataspaceMerge = 0x80423E2B;
	private const uint DataspaceRemove = 0x8042DCE1;
	private const uint DatamapClear = MuiStoreMessageCore.DatamapClearMethod;
	private const uint DatamapFind = MuiStoreMessageCore.DatamapFindMethod;
	private const uint DatamapGet = MuiStoreMessageCore.DatamapGetMethod;
	private const uint DatamapIterate = MuiStoreMessageCore.DatamapIterateMethod;
	private const uint DatamapIterationKey = MuiStoreMessageCore.DatamapIterationKeyMethod;
	private const uint DatamapRemove = MuiStoreMessageCore.DatamapRemoveMethod;
	private const uint DatamapSet = MuiStoreMessageCore.DatamapSetMethod;
	private const uint ObjectmapClear = MuiStoreMessageCore.ObjectmapClearMethod;
	private const uint ObjectmapFind = MuiStoreMessageCore.ObjectmapFindMethod;
	private const uint ObjectmapIterate = MuiStoreMessageCore.ObjectmapIterateMethod;
	private const uint ObjectmapIterationKey = MuiStoreMessageCore.ObjectmapIterationKeyMethod;
	private const uint ObjectmapRemove = MuiStoreMessageCore.ObjectmapRemoveMethod;
	private const uint ObjectmapSet = MuiStoreMessageCore.ObjectmapSetMethod;
	private const uint SemaphoreAttempt = 0x80426CE2;
	private const uint SemaphoreAttemptShared = 0x80422551;
	private const uint SemaphoreObtain = 0x804276F0;
	private const uint SemaphoreObtainShared = 0x8042EA02;
	private const uint SemaphoreRelease = 0x80421F2D;
	private const uint DatamapAutoLockAttribute = 0x8042FBE4;
	private const uint ObjectmapAutoLockAttribute = 0x8042E65F;

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		var lockAttribute = IsDatamapMethod(method) ? DatamapAutoLockAttribute :
			(IsObjectmapMethod(method) ? ObjectmapAutoLockAttribute : 0u);
		uint enabled;
		var locked = lockAttribute != 0 &&
			MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
				lockAttribute, out enabled) && enabled != 0;
		if (locked && !MuiSemaphoreCore.Attempt(ref platform, state, obj)) return 0;
		var result = DispatchCore(ref platform, state, obj, message, method);
		if (locked && !MuiSemaphoreCore.Release(ref platform, state, obj)) return 0;
		return result;
	}

	// Focused native-qualification seam for MUIM_GetConfigItem.  Keeping the
	// packet closure separate from the broad Notify/store dispatcher lets the
	// freestanding artifact prove only the documented configuration query.
	public static uint DispatchGetConfigItem<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiNotifyConfigMessageCore.TryRead(ref platform, message,
			out var packet)) return 0;
		return MuiNotifyCore.GetConfigItem(ref platform, state, obj,
			packet.ConfigId, packet.Storage) ? 1u : 0u;
	}

	// Focused native-qualification seam for the MorphOS Notify UserData
	// family. Each packet is decoded into an explicit fixed-layout message
	// record before the bounded guest-resident tree walk begins.
	public static uint DispatchUserData<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method != MuiNotifyUserDataCore.FindUData &&
			method != MuiNotifyUserDataCore.GetUData &&
			method != MuiNotifyUserDataCore.SetUData &&
			method != MuiNotifyUserDataCore.SetUDataOnce) return 0;
		return DispatchUserDataCore(ref platform, state, obj, message, method);
	}

	// Focused native-qualification seam for the public Notify packet family.
	// Each variable portion is validated by MuiNotifyCore after its fixed
	// header has been decoded into an explicit record.
	public static uint DispatchNotify<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method != Notify && method != KillNotify && method != KillNotifyObj &&
			method != MultiSet && method != FindObject && method != Set &&
			method != NoNotifySet && method != WriteLong &&
			method != WriteString && method != SetAsString &&
			method != CallHook) return 0;
		return DispatchNotifyCore(ref platform, state, obj, message, method);
	}

	// Focused Dataspace packet seam. Each supported packet is decoded by its
	// named ABI record before the existing guest-resident store is called.
	// This is also the smallest useful native qualification root for the
	// Dataspace boundary; IFF packets remain a separate, capability-backed goal.
	public static uint DispatchDataspace<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader)) return 0;
		switch (methodHeader.MethodId)
		{
			case DataspaceAdd:
				if (!MuiDataspaceMessageCore.TryReadAdd(ref platform, message,
					out var add)) return 0;
				return MuiStoreCore.DataspaceAdd(ref platform, state, obj,
					add.Id, add.Data, add.Length) ? 1u : 0u;
			case DataspaceFind:
				if (!MuiDataspaceMessageCore.TryReadFind(ref platform, message,
					out var find)) return 0;
				return MuiStoreCore.DataspaceFind(ref platform, state, obj,
					find.Id).Raw;
			case DataspaceGet:
				if (!MuiDataspaceMessageCore.TryReadGet(ref platform, message,
					out var get)) return 0;
				return MuiStoreCore.DataspaceGet(ref platform, state, obj,
					get.Id, get.SizeStorage).Raw;
			case DataspaceMerge:
				if (!MuiDataspaceMessageCore.TryReadMerge(ref platform, message,
					out var merge)) return 0;
				return MuiStoreCore.DataspaceMerge(ref platform, state, obj,
					merge.Dataspace) ? 1u : 0u;
			case DataspaceRemove:
				if (!MuiDataspaceMessageCore.TryReadRemove(ref platform, message,
					out var remove)) return 0;
				return MuiStoreCore.DataspaceRemove(ref platform, state, obj,
					remove.Id) ? 1u : 0u;
			case DataspaceClear:
				return MuiStoreCore.DataspaceClear(ref platform, state, obj);
		}
		return 0;
	}

	// Focused capability-backed seam for the MorphOS Dataspace IFF methods.
	// IFFParse is intentionally kept out of the frozen headless aggregate: a
	// platform opts into this overload by implementing IMuiIffCapability.
	public static uint DispatchDataspaceIff<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform, IMuiIffCapability
	{
		if (!MuiHeadlessMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader)) return 0;
		switch (methodHeader.MethodId)
		{
			case MuiDataspaceIffMessageCore.ReadIffMethod:
				if (!MuiDataspaceIffMessageCore.TryReadReadIff(ref platform,
					message, out var read)) return 0;
				return unchecked((uint)MuiDataspaceIffCore.ReadIFF(ref platform,
					state, obj, read.Handle));
			case MuiDataspaceIffMessageCore.WriteIffMethod:
				if (!MuiDataspaceIffMessageCore.TryReadWriteIff(ref platform,
					message, out var write)) return 0;
				return unchecked((uint)MuiDataspaceIffCore.WriteIFF(ref platform,
					state, obj, write.Handle, write.Type, write.Id));
		}
		return 0;
	}

	private static uint DispatchNotifyCore<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message, uint method)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (method == FindObject)
		{
			if (!MuiNotifyCore.TryReadFindObject(ref platform, message, method,
				out var packet)) return 0;
			return MuiNotifyCore.FindObject(ref platform, state, obj,
				APTR.FromPointer(packet.FindObject)) ? 1u : 0u;
		}
		if (method == Notify)
		{
			if (!MuiNotifyCore.TryReadNotify(ref platform, message, method,
				out var packet)) return 0;
			return MuiNotifyCore.Add(ref platform, state, obj,
				packet.TriggerAttribute, packet.TriggerValue,
				APTR.FromPointer(packet.Destination), packet.FollowCount,
				MuiNotifyCore.FollowParameters(ref platform, message)) ? 1u : 0u;
		}
		if (method == KillNotify)
		{
			if (!MuiNotifyCore.TryReadKillNotify(ref platform, message, method,
				out var packet)) return 0;
			return MuiNotifyCore.Remove(ref platform, state, obj,
				packet.TriggerAttribute, APTR.Null, false);
		}
		if (method == KillNotifyObj)
		{
			if (!MuiNotifyCore.TryReadKillNotifyObject(ref platform, message, method,
				out var packet)) return 0;
			return MuiNotifyCore.Remove(ref platform, state, obj,
				packet.TriggerAttribute, APTR.FromPointer(packet.Destination), true);
		}
		if (method == MultiSet)
		{
			if (!MuiNotifyCore.TryReadMultiSet(ref platform, message, method,
				out var packet)) return 0;
			return MuiNotifyCore.MultiSet(ref platform, state, obj,
				packet.Attribute, packet.Value,
				APTR.FromPointer(packet.FirstObject),
				MuiNotifyCore.MultiSetVector(ref platform, message)) ? 1u : 0u;
		}
		if (method == WriteLong)
		{
			if (!MuiNotifyWriteCore.TryReadWriteLong(ref platform, message,
				out var writeLong)) return 0;
			return MuiNotifyWriteCore.WriteLong(ref platform, writeLong.Value,
				writeLong.Memory) ? 1u : 0u;
		}
		if (method == WriteString)
		{
			if (!MuiNotifyWriteCore.TryReadWriteString(ref platform, message,
				out var writeString)) return 0;
			return MuiNotifyWriteCore.WriteString(ref platform, writeString.String,
				writeString.Memory) ? 1u : 0u;
		}
		if (method == SetAsString)
			return MuiNotifySetAsStringCore.Apply(ref platform, state, obj,
				message) ? 1u : 0u;
		if (method == CallHook)
			return MuiCallHookCore.Dispatch(ref platform, state, obj, message);
		if (!MuiNotifyCore.TryReadSet(ref platform, message, method,
			out var setPacket)) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			setPacket.Attribute, setPacket.Value, method == Set) ? 1u : 0u;
	}

	private static uint DispatchUserDataCore<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message, uint method)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (method == MuiNotifyUserDataCore.FindUData)
		{
			if (!MuiNotifyUserDataCore.TryReadFind(ref platform, message, method,
				out var packet)) return 0;
			return MuiNotifyUserDataCore.Find(ref platform, state, obj,
				packet.UserData).Raw;
		}
		if (method == MuiNotifyUserDataCore.GetUData)
		{
			if (!MuiNotifyUserDataCore.TryReadGet(ref platform, message, method,
				out var packet)) return 0;
			return MuiNotifyUserDataCore.Get(ref platform, state, obj,
				packet.UserData, packet.Attribute,
				APTR.FromPointer(packet.Storage)) ? 1u : 0u;
		}
		if (!MuiNotifyUserDataCore.TryReadSet(ref platform, message, method,
			out var setPacket)) return 0;
		return MuiNotifyUserDataCore.Set(ref platform, state, obj,
			setPacket.UserData, setPacket.Attribute, setPacket.Value,
			method == MuiNotifyUserDataCore.SetUDataOnce) ? 1u : 0u;
	}

	private static uint DispatchCore<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message, uint method)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
			switch (method)
		{
			case MuiNotifyUserDataCore.FindUData:
			case MuiNotifyUserDataCore.GetUData:
			case MuiNotifyUserDataCore.SetUData:
			case MuiNotifyUserDataCore.SetUDataOnce:
				return DispatchUserDataCore(ref platform, state, obj, message, method);
			case GetConfigItem:
				if (!MuiNotifyConfigMessageCore.TryRead(ref platform, message,
					out var configPacket)) return 0;
				return MuiNotifyCore.GetConfigItem(ref platform, state, obj,
					configPacket.ConfigId, configPacket.Storage) ? 1u : 0u;
			case Notify:
			case CallHook:
			case KillNotify:
			case KillNotifyObj:
			case MultiSet:
			case FindObject:
			case Set:
			case NoNotifySet:
			case WriteLong:
			case WriteString:
			case SetAsString:
				return DispatchNotifyCore(ref platform, state, obj, message, method);
			case Export:
			case Import:
				return DispatchObjectPersistence(ref platform, state, obj, message);
			case FamilyAddHead:
			case FamilyAddTail:
				return MuiFamilyMutationCore.Dispatch(ref platform, state, obj,
					message);
			case FamilyRemove:
				return MuiFamilyMutationCore.Dispatch(ref platform, state, obj,
					message);
			case FamilyInsert:
				return MuiFamilyMutationCore.DispatchInsert(ref platform, state, obj,
					message);
			case FamilyGetChild:
				return MuiFamilyGetChildCore.Dispatch(ref platform, state, obj,
					message);
			case FamilyDoChildMethods:
				return MuiFamilyDoChildMethodsCore.Dispatch(ref platform, state, obj,
					message);
			case FamilyReorder:
				return MuiFamilyMutationCore.DispatchReorder(ref platform, state,
					obj, message);
			case FamilySort:
				return MuiFamilyMutationCore.DispatchSort(ref platform, state, obj,
					message);
			case FamilyTransfer:
				return MuiFamilyMutationCore.DispatchTransfer(ref platform, state,
					obj, message);
			case GroupInitChange:
			case GroupExitChange:
			case GroupExitChange2:
				return MuiGroupChangeCore.Dispatch(ref platform, state, obj,
					message, method);
			case GroupMoveMember:
				if (!MuiGroupOperationsCore.TryReadMoveMember(ref platform,
					message, out var moveMemberPacket)) return 0;
				return MuiGroupOperationsCore.MoveMember(ref platform, state, obj,
					APTR.FromPointer(moveMemberPacket.Object),
					moveMemberPacket.Position) ? 1u : 0u;
			case GroupReorder:
				if (!MuiGroupOperationsCore.TryReadReorder(ref platform, message,
					out var reorderPacket)) return 0;
				return MuiGroupOperationsCore.Reorder(ref platform, state, obj,
					APTR.FromPointer(reorderPacket.After),
					APTR.FromPointer(reorderPacket.Objects)) ? 1u : 0u;
			case GroupSort:
				if (!MuiGroupOperationsCore.TryReadSort(ref platform, message,
					out var sortPacket)) return 0;
				return MuiGroupOperationsCore.Sort(ref platform, state, obj,
					APTR.FromPointer(sortPacket.Objects)) ? 1u : 0u;
			case DataspaceAdd:
				if (!MuiDataspaceMessageCore.TryReadAdd(ref platform, message,
					out var dataspaceAdd)) return 0;
				return MuiStoreCore.DataspaceAdd(ref platform, state, obj,
					dataspaceAdd.Id, dataspaceAdd.Data, dataspaceAdd.Length) ? 1u : 0u;
			case DataspaceFind:
				if (!MuiDataspaceMessageCore.TryReadFind(ref platform, message,
					out var dataspaceFind)) return 0;
				return MuiStoreCore.DataspaceFind(ref platform, state, obj,
					dataspaceFind.Id).Raw;
			case DataspaceGet:
				if (!MuiDataspaceMessageCore.TryReadGet(ref platform, message,
					out var dataspaceGet)) return 0;
				return MuiStoreCore.DataspaceGet(ref platform, state, obj,
					dataspaceGet.Id, dataspaceGet.SizeStorage).Raw;
			case DataspaceMerge:
				if (!MuiDataspaceMessageCore.TryReadMerge(ref platform, message,
					out var dataspaceMerge)) return 0;
				return MuiStoreCore.DataspaceMerge(ref platform, state, obj,
					dataspaceMerge.Dataspace) ? 1u : 0u;
			case DataspaceRemove:
				if (!MuiDataspaceMessageCore.TryReadRemove(ref platform, message,
					out var dataspaceRemove)) return 0;
				return MuiStoreCore.DataspaceRemove(ref platform, state, obj,
					dataspaceRemove.Id) ? 1u : 0u;
			case DataspaceClear:
				return MuiStoreCore.DataspaceClear(ref platform, state, obj);
			case DatamapSet:
			case DatamapFind:
			case DatamapGet:
			case DatamapIterate:
			case DatamapIterationKey:
			case DatamapRemove:
			case DatamapClear:
			case ObjectmapSet:
			case ObjectmapFind:
			case ObjectmapIterate:
			case ObjectmapIterationKey:
			case ObjectmapRemove:
			case ObjectmapClear:
				return MuiStoreMessageCore.Dispatch(ref platform, state, obj, message);
			case SemaphoreAttempt:
				return MuiSemaphoreCore.Attempt(ref platform, state, obj) ? 1u : 0u;
			case SemaphoreAttemptShared:
				return MuiSemaphoreCore.AttemptShared(ref platform, state, obj) ? 1u : 0u;
			case SemaphoreObtain:
				return MuiSemaphoreCore.Obtain(ref platform, state, obj) ? 1u : 0u;
			case SemaphoreObtainShared:
				return MuiSemaphoreCore.ObtainShared(ref platform, state, obj) ? 1u : 0u;
			case SemaphoreRelease:
				return MuiSemaphoreCore.Release(ref platform, state, obj) ? 1u : 0u;
		}
		return 0;
	}

	// Focused native-qualification seam for the MorphOS Notify superclass
	// persistence methods. The exact packet is {MethodID, dataspace}; the core
	// accepts only live MUI objects with a non-zero MUIA_ObjectID and leaves
	// class-specific payload ownership to the native capability.
	public static uint DispatchObjectPersistence<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (!MuiObjectPersistenceMessageCore.TryRead(ref platform, message,
			method, out var packet)) return 0;
		if (method == Export)
			return MuiObjectPersistenceCore.Export(ref platform, state, obj,
				packet.Dataspace) ? 1u : 0u;
		if (method == Import)
			return MuiObjectPersistenceCore.Import(ref platform, state, obj,
				packet.Dataspace) ? 1u : 0u;
		return 0;
	}

	private static bool IsDatamapMethod(uint method) =>
		method == DatamapClear || method == DatamapFind || method == DatamapGet ||
		method == DatamapIterate || method == DatamapIterationKey ||
		method == DatamapRemove || method == DatamapSet;

	private static bool IsObjectmapMethod(uint method) =>
		method == ObjectmapClear || method == ObjectmapFind ||
		method == ObjectmapIterate || method == ObjectmapIterationKey ||
		method == ObjectmapRemove || method == ObjectmapSet;

	private static APTR Pointer<TPlatform>(ref TPlatform platform, APTR packet,
		int offset) where TPlatform : struct, IMuiHeadlessPlatform =>
		APTR.FromPointer(platform.ReadUInt32(packet, offset));
}
