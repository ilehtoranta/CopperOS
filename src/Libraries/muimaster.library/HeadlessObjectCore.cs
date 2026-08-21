/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using Amiga.MUI;

namespace CopperOS.MuiMaster;

public static class MuiHeadlessObjectCore
{
	private const uint ObjectIdAttribute = 0x8042D76E;
	private const uint UserDataAttribute = 0x80420313;
	private const uint ClassPublic = 1;
	private const uint ClassExternal = 2;
	private const uint ClassOwned = 4;
	private const uint ClassBuiltin = 8;
	private const uint ClassVersionDefined = 16;
	private const uint ObjectDisposing = 1;
	// Set only after the creation tag list has been fully applied. Initializer-
	// only MUI attributes can use this marker to distinguish CreateObjectA from
	// later Set/NoNotifySet packets without a managed object shadow.
	internal const uint ObjectInitialized = 2;

	// The named class record already carries the class flags as a fixed-width
	// ULONG. Keep the MorphOS class version/revision metadata in its unused high
	// bits, leaving the low capability flags unchanged. The logical values are
	// exposed through MuiClassVersionMetadata rather than through raw bit math at
	// getter call sites.
	private const uint ClassVersionMask = 0x000FFF00u;
	private const uint ClassRevisionMask = 0xFFF00000u;
	private const int ClassVersionShift = 8;
	private const int ClassRevisionShift = 20;

	public static bool Initialize<TPlatform>(ref TPlatform platform, APTR state)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessMemory.Initialize(ref platform, state);

	public static APTR RegisterClass<TPlatform>(ref TPlatform platform, APTR state,
		APTR className, APTR superClass, ushort instanceSize, APTR dispatcher,
		bool makePublic) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessMemory.Ensure(ref platform, state) || className.IsNull)
			return APTR.Null;
		var existing = FindClassByName(ref platform, state, className);
		if (existing.IsNotNull) return existing;
		var boopsi = platform.MakeClass(className, superClass, instanceSize,
			dispatcher);
		if (boopsi.IsNull) return APTR.Null;
		if (makePublic && !platform.AddClass(boopsi))
		{
			platform.FreeClass(boopsi);
			return APTR.Null;
		}
		var record = MuiHeadlessMemory.Allocate(ref platform,
			MuiHeadlessClassRecord.Size);
		if (record.IsNull)
		{
			if (makePublic) platform.RemoveClass(boopsi);
			platform.FreeClass(boopsi);
			return APTR.Null;
		}
		MuiHeadlessClassRecord classValue = default;
		classValue.Name = className;
		classValue.Boopsi = boopsi;
		classValue.Super = superClass;
		classValue.InstanceSize = instanceSize;
		classValue.Flags = ClassOwned | (makePublic ? ClassPublic : 0);
		if (!MuiHeadlessClassCodec.Write(ref platform, record, classValue))
		{
			if (makePublic) platform.RemoveClass(boopsi);
			platform.FreeClass(boopsi);
			platform.Free(record, MuiHeadlessClassRecord.Size);
			return APTR.Null;
		}
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		classValue.Next = stateValue.Classes;
		if (!MuiHeadlessClassCodec.Write(ref platform, record, classValue))
			return APTR.Null;
		stateValue.Classes = record;
		if (!MuiHeadlessStateCodec.Write(ref platform, state, stateValue))
			return APTR.Null;
		MuiHeadlessMemory.Mutated(ref platform, state);
		return record;
	}

	public static APTR RegisterBuiltinClass<TPlatform>(ref TPlatform platform,
		APTR state, APTR className, APTR superClass, ushort instanceSize,
		APTR dispatcher) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = RegisterClass(ref platform, state, className, superClass,
			instanceSize, dispatcher, true);
		if (record.IsNotNull && MuiHeadlessClassCodec.TryRead(ref platform, record,
			out var classValue))
		{
			classValue.Flags |= ClassBuiltin;
			MuiHeadlessClassCodec.Write(ref platform, record, classValue);
		}
		return record;
	}

	public static APTR RegisterBuiltinClass<TPlatform>(ref TPlatform platform,
		APTR state, APTR className, APTR superClass, ushort instanceSize,
		APTR dispatcher, ushort version, ushort revision)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = RegisterBuiltinClass(ref platform, state, className,
			superClass, instanceSize, dispatcher);
		if (record.IsNotNull && !SetClassVersionRevision(ref platform, record,
			version, revision))
		{
			DeleteClass(ref platform, state, record);
			return APTR.Null;
		}
		return record;
	}

	internal static bool SetClassVersionRevision<TPlatform>(ref TPlatform platform,
		APTR classRecord, uint version, uint revision)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (version > MuiClassVersionMetadata.MaximumValue ||
			revision > MuiClassVersionMetadata.MaximumValue ||
			!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
				out var classValue)) return false;
		var flags = classValue.Flags &
			~(ClassVersionMask | ClassRevisionMask);
		flags |= (version << ClassVersionShift) & ClassVersionMask;
		flags |= (revision << ClassRevisionShift) & ClassRevisionMask;
		flags |= ClassVersionDefined;
		classValue.Flags = flags;
		return MuiHeadlessClassCodec.Write(ref platform, classRecord, classValue);
	}

	internal static bool TryGetClassVersionRevision<TPlatform>(
		ref TPlatform platform, APTR classRecord,
		out MuiClassVersionMetadata value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
			out var classValue) || (classValue.Flags & ClassVersionDefined) == 0)
			return false;
		value.Version = (classValue.Flags & ClassVersionMask) >>
			ClassVersionShift;
		value.Revision = (classValue.Flags & ClassRevisionMask) >>
			ClassRevisionShift;
		return true;
	}

	public static APTR RegisterExternalClass<TPlatform>(ref TPlatform platform,
		APTR state, APTR className, APTR boopsiClass, APTR superClass)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessMemory.Ensure(ref platform, state) || className.IsNull ||
			boopsiClass.IsNull) return APTR.Null;
		var existing = FindClassByName(ref platform, state, className);
		if (existing.IsNotNull) return existing;
		var record = MuiHeadlessMemory.Allocate(ref platform,
			MuiHeadlessClassRecord.Size);
		if (record.IsNull) return APTR.Null;
		MuiHeadlessClassRecord classValue = default;
		classValue.Name = className;
		classValue.Boopsi = boopsiClass;
		classValue.Super = superClass;
		classValue.Flags = ClassExternal;
		if (!MuiHeadlessClassCodec.Write(ref platform, record, classValue))
		{
			platform.Free(record, MuiHeadlessClassRecord.Size);
			return APTR.Null;
		}
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		classValue.Next = stateValue.Classes;
		if (!MuiHeadlessClassCodec.Write(ref platform, record, classValue))
			return APTR.Null;
		stateValue.Classes = record;
		if (!MuiHeadlessStateCodec.Write(ref platform, state, stateValue))
			return APTR.Null;
		MuiHeadlessMemory.Mutated(ref platform, state);
		return record;
	}

	public static APTR RegisterExternalClass<TPlatform>(ref TPlatform platform,
		APTR state, APTR className, APTR boopsiClass, APTR superClass,
		ushort version, ushort revision)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = RegisterExternalClass(ref platform, state, className,
			boopsiClass, superClass);
		if (record.IsNotNull && !SetClassVersionRevision(ref platform, record,
			version, revision))
		{
			DeleteClass(ref platform, state, record);
			return APTR.Null;
		}
		return record;
	}

	public static bool DeleteClass<TPlatform>(ref TPlatform platform, APTR state,
		APTR classRecord) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!ValidClass(ref platform, classRecord) ||
			!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
				out var classValue) || classValue.ObjectCount != 0) return false;
		var boopsi = classValue.Boopsi;
		var flags = classValue.Flags;
		if ((flags & ClassPublic) != 0 && !platform.RemoveClass(boopsi)) return false;
		if ((flags & ClassOwned) != 0 && !platform.FreeClass(boopsi))
		{
			if ((flags & ClassPublic) != 0) platform.AddClass(boopsi);
			return false;
		}
		if (!UnlinkClass(ref platform, state, classRecord)) return false;
		platform.Clear(classRecord, MuiHeadlessClassRecord.Size);
		platform.Free(classRecord, MuiHeadlessClassRecord.Size);
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	public static APTR FindClassByName<TPlatform>(ref TPlatform platform,
		APTR state, APTR className)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessMemory.Ensure(ref platform, state) || className.IsNull)
			return APTR.Null;
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var current = stateValue.Classes;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, current,
				out var classValue)) return APTR.Null;
			var candidate = classValue.Name;
			if (CStringCodec.TryEquals(ref platform, candidate, className, 1024,
				out var equal) && equal) return current;
			current = classValue.Next;
		}
		return APTR.Null;
	}

	public static APTR ClassPointer<TPlatform>(ref TPlatform platform,
		APTR classRecord) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
			out var classValue) ? classValue.Boopsi : APTR.Null;

	public static APTR ObjectClassRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var objectRecord = FindObject(ref platform, state, obj);
		return !MuiHeadlessObjectCodec.TryRead(ref platform, objectRecord,
			out var objectValue) ? APTR.Null : objectValue.Class;
	}

	// Parent links are guest-resident object state, not a second managed
	// hierarchy. Routing code uses this typed seam for bounded active-parent
	// walks and therefore never reconstructs the family tree in host memory.
	internal static APTR ParentObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var objectRecord = FindObject(ref platform, state, obj);
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, objectRecord,
			out var objectValue) || objectValue.Parent.IsNull)
			return APTR.Null;
		return !MuiHeadlessObjectCodec.TryRead(ref platform, objectValue.Parent,
			out var parentValue) ? APTR.Null : parentValue.Boopsi;
	}

	public static APTR CreateObjectA<TPlatform>(ref TPlatform platform,
		APTR state, APTR classRecord, APTR tags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessMemory.Ensure(ref platform, state) ||
			!ValidClass(ref platform, classRecord)) return APTR.Null;
		if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
			out var classValue)) return APTR.Null;
		var boopsiClass = classValue.Boopsi;
		var obj = platform.NewObject(boopsiClass, tags);
		if (obj.IsNull) return APTR.Null;
		var record = MuiHeadlessMemory.Allocate(ref platform,
			MuiHeadlessObjectRecord.Size);
		if (record.IsNull)
		{
			platform.DisposeObject(obj);
			return APTR.Null;
		}
		MuiHeadlessObjectRecord objectValue = default;
		objectValue.Boopsi = obj;
		objectValue.Class = classRecord;
		objectValue.Generation = MuiHeadlessMemory.NextSequence(ref platform, state);
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue))
		{
			platform.DisposeObject(obj);
			platform.Free(record, MuiHeadlessObjectRecord.Size);
			return APTR.Null;
		}
		objectValue.Next = stateValue.Objects;
		if (!MuiHeadlessObjectCodec.Write(ref platform, record, objectValue))
		{
			platform.DisposeObject(obj);
			platform.Free(record, MuiHeadlessObjectRecord.Size);
			return APTR.Null;
		}
		stateValue.Objects = record;
		if (!MuiHeadlessStateCodec.Write(ref platform, state, stateValue))
		{
			platform.DisposeObject(obj);
			platform.Free(record, MuiHeadlessObjectRecord.Size);
			return APTR.Null;
		}
		if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
			out classValue)) return APTR.Null;
		classValue.ObjectCount++;
		MuiHeadlessClassCodec.Write(ref platform, classRecord, classValue);
		if (!ApplyTags(ref platform, state, record, tags))
		{
			DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var initializedValue))
		{
			DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}
		initializedValue.Flags |= ObjectInitialized;
		if (!MuiHeadlessObjectCodec.Write(ref platform, record,
			initializedValue))
		{
			DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}
		MuiHeadlessMemory.Mutated(ref platform, state);
		return obj;
	}

	internal static bool IsObjectInitialized<TPlatform>(ref TPlatform platform,
		APTR record) where TPlatform : struct, IMuiHeadlessPlatform
	{
		return MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue) &&
			(objectValue.Flags & ObjectInitialized) != 0;
	}

	public static bool DisposeObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		if ((objectValue.Flags & ObjectDisposing) != 0) return false;
		objectValue.Flags |= ObjectDisposing;
		MuiHeadlessObjectCodec.Write(ref platform, record, objectValue);
		// Handled-events state owns its generated guest MUI_EventHandlerNode.
		// Release that registration while the object and its parent links are
		// still valid; StoreCore.ClearAll below then only removes the copied
		// state bytes.
		MuiAreaEventHandlerCore.Cleanup(ref platform, state, obj);
		MuiFamilyCore.RemoveAllChildren(ref platform, state, record, true);
		MuiFamilyCore.DetachFromParent(ref platform, state, record);
		MuiApplicationWindowCore.CleanupRecords(ref platform, state, obj);
		MuiGroupChangeCore.CleanupRecords(ref platform, state, obj);
		MuiGroupPageCore.Cleanup(ref platform, state, obj);
		MuiGroupChildrenCore.Cleanup(ref platform, state, obj);
		MuiGroupLayoutHookCore.Cleanup(ref platform, state, obj);
		MuiApplicationWindowListCore.Cleanup(ref platform, state, obj);
		FreeObjectAttributes(ref platform, record);
		MuiNotifyCore.RemoveAll(ref platform, state, record);
		MuiStoreCore.ClearAll(ref platform, record);
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out objectValue)) return false;
		var classRecord = objectValue.Class;
		platform.DisposeObject(obj);
		UnlinkObject(ref platform, state, record);
		if (MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
			out var classValue))
		{
			if (classValue.ObjectCount != 0) classValue.ObjectCount--;
			MuiHeadlessClassCodec.Write(ref platform, classRecord, classValue);
		}
		platform.Clear(record, MuiHeadlessObjectRecord.Size);
		platform.Free(record, MuiHeadlessObjectRecord.Size);
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	public static APTR FindObject<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessMemory.Ensure(ref platform, state) || obj.IsNull)
			return APTR.Null;
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var current = stateValue.Objects;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, current,
				out var objectValue))
				return APTR.Null;
			if (objectValue.Boopsi.Raw == obj.Raw) return current;
			current = objectValue.Next;
		}
		return APTR.Null;
	}

	public static bool SetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		return SetRecordAttribute(ref platform, state, record, attribute, value,
			notify);
	}

	public static bool GetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		var record = FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		if (MuiObjectMetadataCore.TryGet(ref platform, state, obj, attribute,
			out value)) return true;
		// Collection classes own their public projections, including the
		// Listview interaction-policy record. Give that typed getter a chance
		// before the generic attribute list so OM_GET and direct Get share the
		// same guest-resident struct boundary. ListviewCore falls back to this
		// method only for forwarded child attributes, so the class-gated call is
		// non-recursive for its own policy values.
		if (MuiListCore.Classify(ref platform, state, obj) ==
			MuiCollectionClass.Listview &&
			MuiListviewCore.IsInteractionPolicyAttribute(attribute) &&
			MuiListviewCore.GetAttribute(ref platform, state, obj, attribute,
				out value)) return true;
		if (MuiListCore.Classify(ref platform, state, obj) ==
			MuiCollectionClass.Floattext &&
			MuiFloattextCore.IsStateAttribute(attribute) &&
			MuiFloattextCore.GetAttribute(ref platform, state, obj, attribute,
				out value)) return true;
		if (MuiListCore.Classify(ref platform, state, obj) ==
			MuiCollectionClass.Stringscroll &&
			MuiStringscrollCore.IsPublicGetterAttribute(attribute) &&
			MuiStringscrollCore.GetAttribute(ref platform, state, obj, attribute,
				out value)) return true;
		if (MuiListtreeCore.IsListtree(ref platform, state, obj) &&
			MuiListtreeCore.IsPublicGetterAttribute(attribute) &&
			MuiListtreeCore.GetAttribute(ref platform, state, obj, attribute,
				out value)) return true;
		var collection = MuiListCore.Classify(ref platform, state, obj);
		if ((collection == MuiCollectionClass.Dirlist ||
			collection == MuiCollectionClass.Volumelist) &&
			(collection == MuiCollectionClass.Volumelist
				? MuiVolumelistCore.IsPublicGetterAttribute(attribute)
				: MuiDirlistCore.IsPublicGetterAttribute(attribute)) &&
			(collection == MuiCollectionClass.Volumelist
				? MuiVolumelistCore.GetAttribute(ref platform, state, obj, attribute,
					out value)
				: MuiDirlistCore.GetAttribute(ref platform, state, obj, attribute,
					out value))) return true;
		var handled = false;
		if (MuiWindowPublicCore.TryGet(ref platform, state, obj, attribute,
			out value, out handled) && handled) return true;
		if (handled) return false;
		if (MuiApplicationMessageCore.TryGet(ref platform, state, obj,
			attribute, out value, out handled) && handled) return true;
		if (handled) return false;
		if (MuiApplicationWindowCore.TryGet(ref platform, state, obj,
			attribute, out value, out handled) && handled) return true;
		if (handled) return false;
		if (MuiApplicationCommandsCore.TryGet(ref platform, state, obj,
			attribute, out value, out handled) && handled) return true;
		if (handled) return false;
		if (MuiApplicationWindowListCore.TryGet(ref platform, state, obj,
			attribute, out value, out handled) && handled) return true;
		if (handled) return false;
		if (MuiGroupChildrenCore.TryGet(ref platform, state, obj, attribute,
			out value, out handled) && handled) return true;
		if (handled) return false;
		if (MuiGroupChildrenCore.TryGetFamily(ref platform, state, obj,
			attribute, out value, out handled) && handled) return true;
		if (handled) return false;
		if (MuiCommonControlCore.TryGet(ref platform, state, obj, attribute,
			out value, out handled) && handled) return true;
		if (handled) return false;
		if (attribute == ObjectIdAttribute || attribute == UserDataAttribute)
		{
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var objectValue)) return false;
			value = attribute == ObjectIdAttribute ? objectValue.ObjectId :
				objectValue.UserData;
			return true;
		}
		var item = FindAttribute(ref platform, record, attribute);
		if (item.IsNull) return false;
		if (!MuiHeadlessAttributeCodec.TryRead(ref platform, item,
			out var attributeValue)) return false;
		value = attributeValue.Value;
		return true;
	}

	internal static bool GetAttributeList<TPlatform>(ref TPlatform platform,
		APTR attributes, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		var current = attributes;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessAttributeCodec.TryRead(ref platform, current,
				out var attributeValue)) return false;
			if (attributeValue.Id == attribute)
			{
				value = attributeValue.Value;
				return true;
			}
			current = attributeValue.Next;
		}
		return false;
	}

	internal static bool SetRecordAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var handled = false;
		if (MuiWindowPublicCore.TrySet(ref platform, state, record, attribute,
			value, notify, out handled) && handled) return true;
		if (handled) return false;
		if (MuiApplicationMessageCore.TrySet(ref platform, state, record,
			attribute, value, notify, out handled) && handled) return true;
		if (handled) return false;
		if (MuiApplicationCommandsCore.TrySet(ref platform, state, record,
			attribute, value, notify, out handled) && handled) return true;
		if (handled) return false;
		if (MuiApplicationWindowListCore.TrySet(ref platform, state, record,
			attribute, value, notify, out handled) && handled) return true;
		if (handled) return false;
		if (MuiGroupChildrenCore.TrySet(ref platform, state, record, attribute,
			value, notify, out handled) && handled) return true;
		if (handled) return false;
		if (MuiGroupPageCore.TrySet(ref platform, state, record, attribute, value,
			notify, out handled) && handled) return true;
		if (handled) return false;
		if (MuiGroupLayoutHookCore.TrySet(ref platform, state, record, attribute,
			value, notify, out handled) && handled) return true;
		if (handled) return false;
		return SetRecordAttributeRaw(ref platform, state, record, attribute, value,
			notify);
	}

	internal static bool SetRecordAttributeRaw<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (attribute == ObjectIdAttribute || attribute == UserDataAttribute)
		{
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var objectValue)) return false;
			if (attribute == ObjectIdAttribute) objectValue.ObjectId = value;
			else objectValue.UserData = value;
			MuiHeadlessObjectCodec.Write(ref platform, record, objectValue);
		}
		else
		{
			var item = FindAttribute(ref platform, record, attribute);
			if (item.IsNull)
			{
				item = MuiHeadlessMemory.Allocate(ref platform,
					MuiHeadlessAttributeRecord.Size);
				if (item.IsNull) return false;
				if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
					out var objectValue))
				{
					platform.Free(item, MuiHeadlessAttributeRecord.Size);
					return false;
				}
				MuiHeadlessAttributeRecord attributeValue = default;
				attributeValue.Id = attribute;
				attributeValue.Next = objectValue.Attributes;
				if (!MuiHeadlessAttributeCodec.Write(ref platform, item,
					attributeValue))
				{
					platform.Free(item, MuiHeadlessAttributeRecord.Size);
					return false;
				}
				objectValue.Attributes = item;
				if (!MuiHeadlessObjectCodec.Write(ref platform, record,
					objectValue))
				{
					platform.Free(item, MuiHeadlessAttributeRecord.Size);
					return false;
				}
			}
			if (!MuiHeadlessAttributeCodec.TryRead(ref platform, item,
				out var currentValue)) return false;
			currentValue.Value = value;
			currentValue.Generation = MuiHeadlessMemory.NextSequence(
				ref platform, state);
			if (!MuiHeadlessAttributeCodec.Write(ref platform, item,
				currentValue)) return false;
		}
		MuiHeadlessMemory.Mutated(ref platform, state);
		if (notify) MuiNotifyCore.DispatchAttributeChange(ref platform, state,
			record, attribute, value);
		return true;
	}

	internal static bool SetExistingAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		if (attribute == ObjectIdAttribute || attribute == UserDataAttribute)
			return SetRecordAttributeRaw(ref platform, state, record, attribute,
				value, false);
		if (FindAttribute(ref platform, record, attribute).IsNull) return false;
		return SetRecordAttributeRaw(ref platform, state, record, attribute, value,
			false);
	}

	internal static bool GetRawAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		var record = FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		if (attribute == ObjectIdAttribute || attribute == UserDataAttribute)
		{
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var objectValue)) return false;
			value = attribute == ObjectIdAttribute ? objectValue.ObjectId :
				objectValue.UserData;
			return true;
		}
		var item = FindAttribute(ref platform, record, attribute);
		if (item.IsNull) return false;
		if (!MuiHeadlessAttributeCodec.TryRead(ref platform, item,
			out var attributeValue)) return false;
		value = attributeValue.Value;
		return true;
	}

	private static bool ApplyTags<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, APTR tags) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		uint visited = 0;
		while (cursor.Base.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiAslTagItemVectorCodec.TryGetEntry(ref platform, cursor,
				out var current) || !MuiAslTagItemCodec.TryRead(ref platform, current,
				out var item)) return false;
			var tag = item.Tag;
			var data = item.Data;
			if (tag == MuiAslTagListCore.TagDone) return true;
			if (tag == MuiAslTagListCore.TagIgnore)
			{
				if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
					return false;
				continue;
			}
			if (tag == MuiAslTagListCore.TagMore)
			{
				cursor.Base = APTR.FromPointer(data);
				cursor.Index = 0;
				continue;
			}
			if (tag == MuiAslTagListCore.TagSkip)
			{
				if (data == uint.MaxValue ||
					!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, data + 1))
					return false;
				continue;
			}
			if (!SetRecordAttribute(ref platform, state, record, tag, data, false))
				return false;
			if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
				return false;
		}
		return cursor.Base.IsNull;
	}

	private static APTR FindAttribute<TPlatform>(ref TPlatform platform,
		APTR record, uint attribute)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return APTR.Null;
		var current = objectValue.Attributes;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessAttributeCodec.TryRead(ref platform, current,
				out var attributeValue)) return APTR.Null;
			if (attributeValue.Id == attribute) return current;
			current = attributeValue.Next;
		}
		return APTR.Null;
	}

	private static bool ValidClass<TPlatform>(ref TPlatform platform, APTR record)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessClassCodec.TryRead(ref platform, record,
			out var classValue) && classValue.Boopsi.IsNotNull;

	private static bool UnlinkClass<TPlatform>(ref TPlatform platform, APTR state,
		APTR target) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return false;
		var current = stateValue.Classes;
		var previous = APTR.Null;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, current,
				out var classValue)) return false;
			if (current.Raw == target.Raw)
			{
				if (previous.IsNull)
				{
					stateValue.Classes = classValue.Next;
					if (!MuiHeadlessStateCodec.Write(ref platform, state,
						stateValue)) return false;
				}
				else
				{
					if (!MuiHeadlessClassCodec.TryRead(ref platform, previous,
						out var previousValue)) return false;
					previousValue.Next = classValue.Next;
					MuiHeadlessClassCodec.Write(ref platform, previous,
						previousValue);
				}
				return true;
			}
			previous = current;
			current = classValue.Next;
		}
		return false;
	}

	private static bool UnlinkObject<TPlatform>(ref TPlatform platform, APTR state,
		APTR target) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return false;
		var current = stateValue.Objects;
		var previous = APTR.Null;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, current,
				out var objectValue)) return false;
			if (current.Raw == target.Raw)
			{
				if (previous.IsNull)
				{
					stateValue.Objects = objectValue.Next;
					if (!MuiHeadlessStateCodec.Write(ref platform, state,
						stateValue)) return false;
				}
				else
				{
					if (!MuiHeadlessObjectCodec.TryRead(ref platform, previous,
						out var previousValue)) return false;
					previousValue.Next = objectValue.Next;
					MuiHeadlessObjectCodec.Write(ref platform, previous,
						previousValue);
				}
				return true;
			}
			previous = current;
			current = objectValue.Next;
		}
		return false;
	}

	private static void FreeObjectAttributes<TPlatform>(ref TPlatform platform,
		APTR record) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return;
		var current = objectValue.Attributes;
		objectValue.Attributes = APTR.Null;
		MuiHeadlessObjectCodec.Write(ref platform, record, objectValue);
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessAttributeCodec.TryRead(ref platform, current,
				out var attributeValue)) return;
			var next = attributeValue.Next;
			platform.Clear(current, MuiHeadlessAttributeRecord.Size);
			platform.Free(current, MuiHeadlessAttributeRecord.Size);
			current = next;
		}
	}

	internal static bool Unlink<TPlatform>(ref TPlatform platform, APTR owner,
		int headOffset, APTR target, int nextOffset)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = APTR.FromPointer(platform.ReadUInt32(owner, headOffset));
		var previous = APTR.Null;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (current.Raw == target.Raw)
			{
				var next = platform.ReadUInt32(current, nextOffset);
				if (previous.IsNull) platform.WriteUInt32(owner, headOffset, next);
				else platform.WriteUInt32(previous, nextOffset, next);
				return true;
			}
			previous = current;
			current = APTR.FromPointer(platform.ReadUInt32(current, nextOffset));
		}
		return false;
	}

	internal static void FreeList<TPlatform>(ref TPlatform platform, APTR owner,
		int headOffset, int nextOffset, uint size)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = APTR.FromPointer(platform.ReadUInt32(owner, headOffset));
		platform.WriteUInt32(owner, headOffset, 0);
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!platform.IsMapped(current, size)) return;
			var next = APTR.FromPointer(platform.ReadUInt32(current, nextOffset));
			platform.Clear(current, size);
			platform.Free(current, size);
			current = next;
		}
	}
}
