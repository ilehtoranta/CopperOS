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
    private const uint NoNotifyAttribute = 0x804237F9;
    private const uint NoNotifyMethodAttribute = 0x80420A74;
    private const uint ClassPublic = 1;
    internal const uint ClassExternal = 2;
    private const uint ClassOwned = 4;
    private const uint ClassBuiltin = 8;
    private const uint ClassVersionDefined = 16;
    internal const uint ObjectDisposing = 1;
    internal const uint ObjectProviderBusy = 4;
    private const uint ObjectAwaitingFontCleanup = 8;
    private const uint ObjectSidecarOnlyDisposal = 16;
    private const uint ObjectAwaitingChildCleanup = 32;
    // Pending remains set on failed scrollbar construction until disposal.
    // Three populated links alone do not establish successful initialization.
    internal const uint ObjectScrollbarConstructionPending = 64;
    internal const uint ObjectScrollbarConstructionComplete = 128;
    internal const uint ObjectConstructionBinding = 256;
    internal const uint ObjectControlConstructionActive = 512;
    internal const uint ObjectRadioConstructionPending = 1024;
    internal const uint ObjectRadioConstructionComplete = 2048;
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
        where TPlatform : struct, IMuiGuestMemory =>
        MuiHeadlessMemory.Initialize(ref platform, state);

    public static APTR RegisterClass<TPlatform>(ref TPlatform platform, APTR state,
        APTR className, APTR superClass, ushort instanceSize, APTR dispatcher,
        bool makePublic) where TPlatform : struct, IMuiClassRegistryPlatform
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
        APTR dispatcher) where TPlatform : struct, IMuiClassRegistryPlatform
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
        where TPlatform : struct, IMuiClassRegistryPlatform
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
        where TPlatform : struct, IMuiAllocationPlatform
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

    // Native custom classes may be intentionally unnamed: Intuition creates
    // them with a Null class ID, while the MUI sidecar still needs a
    // declaration-ordered registry record for superclass admission and
    // lifecycle ownership. Keep this path separate from the public name-based
    // RegisterExternalClass contract so no fake class name is invented.
    internal static APTR RegisterNativeClass<TPlatform>(ref TPlatform platform,
        APTR state, APTR boopsiClass, APTR superClass, ushort instanceSize)
        where TPlatform : struct, IMuiAllocationPlatform
    {
        if (!MuiHeadlessMemory.Ensure(ref platform, state) ||
            boopsiClass.IsNull ||
            !MuiHeadlessStateCodec.TryRead(ref platform, state,
                out var stateValue)) return APTR.Null;

        var current = stateValue.Classes;
        uint visited = 0;
        while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
        {
            if (!MuiHeadlessClassCodec.TryRead(ref platform, current,
                out var currentValue)) return APTR.Null;
            // A class pointer is the native identity. Do not publish a second
            // anonymous record that could make teardown ambiguous.
            if (currentValue.Boopsi == boopsiClass) return APTR.Null;
            current = currentValue.Next;
        }
        if (current.IsNotNull) return APTR.Null;

        var record = MuiHeadlessMemory.Allocate(ref platform,
            MuiHeadlessClassRecord.Size);
        if (record.IsNull) return APTR.Null;
        var classValue = default(MuiHeadlessClassRecord);
        classValue.Name = APTR.Null;
        classValue.Boopsi = boopsiClass;
        classValue.Super = superClass;
        classValue.InstanceSize = instanceSize;
        classValue.Flags = ClassExternal;
        classValue.Next = stateValue.Classes;
        if (!MuiHeadlessClassCodec.Write(ref platform, record, classValue))
        {
            platform.Free(record, MuiHeadlessClassRecord.Size);
            return APTR.Null;
        }

        stateValue.Classes = record;
        if (!MuiHeadlessStateCodec.Write(ref platform, state, stateValue))
        {
            platform.Clear(record, MuiHeadlessClassRecord.Size);
            platform.Free(record, MuiHeadlessClassRecord.Size);
            return APTR.Null;
        }
        MuiHeadlessMemory.Mutated(ref platform, state);
        return record;
    }

    public static APTR RegisterExternalClass<TPlatform>(ref TPlatform platform,
        APTR state, APTR className, APTR boopsiClass, APTR superClass,
        ushort version, ushort revision)
        where TPlatform : struct, IMuiClassRegistryPlatform
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
        APTR classRecord) where TPlatform : struct, IMuiClassRegistryPlatform
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
        where TPlatform : struct, IMuiGuestMemory
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
        APTR classRecord) where TPlatform : struct, IMuiGuestMemory =>
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
        => CreateObjectInReservedSlot(ref platform, state, classRecord, tags, APTR.Null);

    // constructionObjectField is a pre-admitted destination in an owner-pinned
    // empty cleanup slot. Publish registration there before fallible initialization.
    internal static APTR CreateObjectInReservedSlot<TPlatform>(ref TPlatform platform,
        APTR state, APTR classRecord, APTR tags, APTR constructionObjectField)
        where TPlatform : struct, IMuiHeadlessPlatform
    {
        if (!MuiHeadlessMemory.Ensure(ref platform, state) ||
            !ValidClass(ref platform, classRecord)) return APTR.Null;
        if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
            out var classValue)) return APTR.Null;
        var boopsiClass = classValue.Boopsi;
        var obj = platform.NewObject(boopsiClass, tags);
        if (obj.IsNull) return APTR.Null;
        return CompleteConstructedObject(ref platform, state, classRecord, obj, tags,
            constructionObjectField);
    }

    // A constructor may already have performed MUI initialization. Reuse only
    // a complete sidecar for the expected class; never apply its tags twice.
    internal static APTR CompleteConstructedObject<TPlatform>(ref TPlatform platform,
        APTR state, APTR classRecord, APTR obj, APTR tags)
        where TPlatform : struct, IMuiHeadlessPlatform
        => CompleteConstructedObject(ref platform, state, classRecord, obj, tags, APTR.Null);

    private static APTR CompleteConstructedObject<TPlatform>(ref TPlatform platform,
        APTR state, APTR classRecord, APTR obj, APTR tags, APTR constructionObjectField)
        where TPlatform : struct, IMuiHeadlessPlatform
    {
        if (!TryFindObject(ref platform, state, obj, out var existing)) return APTR.Null;
        if (existing.IsNotNull)
        {
            if (!MuiHeadlessObjectCodec.TryRead(ref platform, existing, out var value) ||
                value.Class != classRecord || (value.Flags & ObjectInitialized) == 0 ||
                (value.Flags & ObjectDisposing) != 0) return APTR.Null;
            if (constructionObjectField.IsNotNull)
            {
                // A constructor-attached fresh sidecar may be reused, but a
                // returned alias must not acquire a second cleanup owner.
                if (value.Parent.IsNotNull ||
                    (value.Flags & (ObjectProviderBusy | ObjectConstructionBinding)) != 0 ||
                    !MuiFamilyCore.AdmitsChildDisposal(ref platform, state, existing, APTR.Null))
                    return APTR.Null;
                platform.WriteUInt32(constructionObjectField, 0, existing.Raw);
            }
            return obj;
        }
        return AttachConstructedObject(ref platform, state, classRecord, obj, tags,
            out _, constructionObjectField);
    }

    // Takes ownership of a successfully constructed BOOPSI object. The caller
    // must hold the service scope. Duplicate attachment is rejected without
    // consuming the existing object's ownership.
    // An unreadable registry cannot establish ownership and leaves the object
    // untouched. After absence is established, failure disposes the transferred
    // object. This is not an OM_NEW dispatcher.
    internal static APTR AttachConstructedObject<TPlatform>(ref TPlatform platform,
        APTR state, APTR classRecord, APTR obj, APTR tags)
        where TPlatform : struct, IMuiHeadlessPlatform
        => AttachConstructedObject(ref platform, state, classRecord, obj, tags, out _);

    internal static APTR AttachConstructedObject<TPlatform>(ref TPlatform platform,
        APTR state, APTR classRecord, APTR obj, APTR tags,
        out MuiObjectAttachmentOwnership ownership)
        where TPlatform : struct, IMuiHeadlessPlatform
        => AttachConstructedObject(ref platform, state, classRecord, obj, tags,
            out ownership, APTR.Null);

    private static APTR AttachConstructedObject<TPlatform>(ref TPlatform platform,
        APTR state, APTR classRecord, APTR obj, APTR tags,
        out MuiObjectAttachmentOwnership ownership, APTR constructionObjectField,
        bool retainForDispatcher = false)
        where TPlatform : struct, IMuiHeadlessPlatform
    {
        ownership = MuiObjectAttachmentOwnership.Unresolved;
        if (obj.IsNull) return APTR.Null;
        if (!TryFindObject(ref platform, state, obj, out var existing)) return APTR.Null;
        if (existing.IsNotNull)
        {
            ownership = MuiObjectAttachmentOwnership.AlreadyRegistered;
            return APTR.Null;
        }
        ownership = retainForDispatcher ? MuiObjectAttachmentOwnership.CallerOwned :
            MuiObjectAttachmentOwnership.Transferred;
        if (!ValidClass(ref platform, classRecord) ||
            !MuiHeadlessClassCodec.TryRead(ref platform, classRecord, out var admittedClass) ||
            admittedClass.ObjectCount == uint.MaxValue)
        {
            if (!retainForDispatcher) platform.DisposeObject(obj);
            return APTR.Null;
        }
        var record = MuiHeadlessMemory.Allocate(ref platform,
            MuiHeadlessObjectRecord.Size);
        // Allocation may reenter and attach this same native object. The
        // pre-allocation absence proof is no longer sufficient for publication
        // or disposal, even if allocation fails. Keep the callback's sidecar
        // and native lifetime intact.
        var foundAfterAllocation = TryFindObject(ref platform, state, obj, out existing);
        if (!foundAfterAllocation || existing.IsNotNull)
        {
            ownership = foundAfterAllocation ? MuiObjectAttachmentOwnership.AlreadyRegistered :
                MuiObjectAttachmentOwnership.Unresolved;
            if (record.IsNotNull) platform.Free(record, MuiHeadlessObjectRecord.Size);
            return APTR.Null;
        }
        if (record.IsNull)
        {
            if (!retainForDispatcher) platform.DisposeObject(obj);
            return APTR.Null;
        }
        MuiHeadlessObjectRecord objectValue = default;
        objectValue.Boopsi = obj;
        objectValue.Class = classRecord;
        objectValue.Generation = MuiHeadlessMemory.NextSequence(ref platform, state);
        if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
            out var stateValue))
        {
            if (!retainForDispatcher) platform.DisposeObject(obj);
            platform.Free(record, MuiHeadlessObjectRecord.Size);
            return APTR.Null;
        }
        objectValue.Next = stateValue.Objects;
        if (!MuiHeadlessObjectCodec.Write(ref platform, record, objectValue))
        {
            if (!retainForDispatcher) platform.DisposeObject(obj);
            platform.Free(record, MuiHeadlessObjectRecord.Size);
            return APTR.Null;
        }
        // Admit both visible destinations before changing either. No allocator
        // or dispatcher call may intervene between admission and these writes.
        if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord, out var classValue) ||
            classValue.ObjectCount == uint.MaxValue ||
            !MuiHeadlessStateMemoryCodec.TryGetAddress(ref platform, state,
                MuiHeadlessStateField.Objects, out var objectsField) ||
            !MuiHeadlessClassMemoryCodec.TryGetAddress(ref platform, classRecord,
                MuiHeadlessClassField.ObjectCount, out var countField))
        {
            if (!retainForDispatcher) platform.DisposeObject(obj);
            platform.Free(record, MuiHeadlessObjectRecord.Size);
            return APTR.Null;
        }
        platform.WriteUInt32(countField, 0, classValue.ObjectCount + 1);
        platform.WriteUInt32(objectsField, 0, record.Raw);
        ownership = MuiObjectAttachmentOwnership.Transferred;
        if (constructionObjectField.IsNotNull)
            platform.WriteUInt32(constructionObjectField, 0, record.Raw);
        if (!ApplyTags(ref platform, state, record, tags))
        {
            if (!retainForDispatcher && constructionObjectField.IsNull) DisposeObject(ref platform, state, obj);
            return APTR.Null;
        }
        if (!MuiStoreCore.InitializeStorePool(ref platform, state, record))
        {
            if (!retainForDispatcher && constructionObjectField.IsNull) DisposeObject(ref platform, state, obj);
            return APTR.Null;
        }
        if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
            out var initializedValue))
        {
            if (!retainForDispatcher && constructionObjectField.IsNull) DisposeObject(ref platform, state, obj);
            return APTR.Null;
        }
        initializedValue.Flags |= ObjectInitialized;
        if (!MuiHeadlessObjectCodec.Write(ref platform, record,
            initializedValue))
        {
            if (!retainForDispatcher && constructionObjectField.IsNull) DisposeObject(ref platform, state, obj);
            return APTR.Null;
        }
        MuiHeadlessMemory.Mutated(ref platform, state);
        return obj;
    }

    // Constructor dispatch owns native destruction. Never invoke DisposeObject
    // implicitly here: before publication ownership stays with the caller; after
    // publication the registered partial sidecar retains recovery authority.
    internal static APTR AttachConstructedObjectForDispatcher<TPlatform>(ref TPlatform platform,
        APTR state, APTR classRecord, APTR obj, APTR tags,
        out MuiObjectAttachmentOwnership ownership)
        where TPlatform : struct, IMuiHeadlessPlatform
        => AttachConstructedObject(ref platform, state, classRecord, obj, tags,
            out ownership, APTR.Null, true);

    internal static bool IsObjectInitialized<TPlatform>(ref TPlatform platform,
        APTR record) where TPlatform : struct, IMuiHeadlessPlatform
    {
        return MuiHeadlessObjectCodec.TryRead(ref platform, record,
            out var objectValue) &&
            (objectValue.Flags & ObjectInitialized) != 0;
    }

    public static bool DisposeObject<TPlatform>(ref TPlatform platform,
        APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
        => DisposeObjectState(ref platform, state, obj, true);

    // Constructor/destructor dispatch owns the native superclass call when
    // disposeNative is false. This routine then releases only MUI-owned state.
    internal static bool DisposeObjectState<TPlatform>(ref TPlatform platform,
        APTR state, APTR obj, bool disposeNative)
        where TPlatform : struct, IMuiHeadlessPlatform
        => DisposeOwnedObjectState(ref platform, state, obj, disposeNative, APTR.Null);

    internal static bool DisposeOwnedObjectState<TPlatform>(ref TPlatform platform,
        APTR state, APTR obj, bool disposeNative, APTR cleanupOwner)
        where TPlatform : struct, IMuiHeadlessPlatform
    {
        var record = FindObject(ref platform, state, obj);
        if (record.IsNull) return false;
        if (!MuiFamilyCore.AdmitsChildDisposal(ref platform, state, record,
            cleanupOwner)) return false;
        if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
            out var objectValue)) return false;
        if ((objectValue.Flags & (ObjectProviderBusy | ObjectConstructionBinding |
            ObjectControlConstructionActive)) != 0) return false;
        var resuming = (objectValue.Flags & ObjectDisposing) != 0;
        var resumeFont = resuming && (objectValue.Flags & ObjectAwaitingFontCleanup) != 0;
        var resumeChildren = resuming && (objectValue.Flags & ObjectAwaitingChildCleanup) != 0;
        if (resuming && ((!resumeFont && !resumeChildren) ||
            ((objectValue.Flags & ObjectSidecarOnlyDisposal) == 0) != disposeNative))
            return false;
        if (!MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, record,
            MuiHeadlessObjectField.Flags, out var flagsField)) return false;
        var disposalFlags = (objectValue.Flags | ObjectDisposing) &
            ~(ObjectAwaitingFontCleanup | ObjectAwaitingChildCleanup);
        if (!disposeNative) disposalFlags |= ObjectSidecarOnlyDisposal;
        platform.WriteUInt32(flagsField, 0, disposalFlags);
        if (!resuming)
        {
            // Notifications may target this object explicitly or through the
            // MorphOS self/ancestor destination tokens. Remove those recipes while
            // the full guest object tree is still valid; the notification records
            // themselves remain guest-resident named records and require no managed
            // destination table.
            MuiNotifyCore.RemoveAllToObject(ref platform, state, obj);
            // External Listview scroller recipes retain destination object pointers
            // in named guest records. Remove any recipe targeting this object before
            // its record or child topology can disappear.
            MuiListviewCore.DisconnectExternalScrollerConnectionsToObject(
                ref platform, state, obj);
            // String.mui AttachedList relationships are caller-owned guest pointers;
            // clear matching typed records before an independently disposed Listview
            // can leave a stale navigation target behind.
            MuiCommonControlCore.DisconnectStringAttachedListConnectionsToObject(
                ref platform, state, obj);
            // Handled-events state owns its generated guest MUI_EventHandlerNode.
            // Release that registration while the object and its parent links are
            // still valid; StoreCore.ClearAll below then only removes the copied
            // state bytes.
            MuiAreaEventHandlerCore.Cleanup(ref platform, state, obj);
        }
        if (!resumeFont)
        {
            if (!MuiFamilyCore.RemoveAllChildren(ref platform, state, record, true))
            {
                if (MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, record,
                    MuiHeadlessObjectField.Flags, out flagsField))
                    platform.WriteUInt32(flagsField, 0,
                        platform.ReadUInt32(flagsField, 0) | ObjectAwaitingChildCleanup);
                return false;
            }
            MuiFamilyCore.DetachFromParent(ref platform, state, record);
            MuiApplicationWindowCore.CleanupRecords(ref platform, state, obj);
            MuiGroupChangeCore.CleanupRecords(ref platform, state, obj);
            MuiGroupPageCore.Cleanup(ref platform, state, obj);
            MuiGroupChildrenCore.Cleanup(ref platform, state, obj);
            MuiGroupLayoutHookCore.Cleanup(ref platform, state, obj);
            MuiApplicationWindowListCore.Cleanup(ref platform, state, obj);
            // Collection specialists own additional guest-resident records beyond
            // the generic object Dataspace. Keep this direct-disposal path aligned
            // with the collection lifecycle wrapper so a caller cannot strand a
            // Listview/Listtree/List/Stringscroll record by bypassing OM_DISPOSE.
            var collectionClass = MuiListCore.Classify(ref platform, state, obj);
            if (MuiListtreeCore.IsListtree(ref platform, state, obj))
                MuiListtreeCore.CleanupRecords(ref platform, state, obj);
            else if (collectionClass == MuiCollectionClass.Listview)
                MuiListviewCore.CleanupRecords(ref platform, state, obj);
            else if (collectionClass == MuiCollectionClass.Stringscroll)
                MuiStringscrollCore.Cleanup(ref platform, state, obj);
            else if (MuiListCore.IsListBacked(collectionClass))
                MuiListCore.CleanupRecords(ref platform, state, obj);
            // Area drag owns a guest-resident typed state block behind a private
            // attribute. Release it before the generic attribute nodes are freed.
            MuiAreaDragCore.Cleanup(ref platform, state, obj);
        }
        // A setup Area owns the provider font returned for its CustomFont spec.
        // Close that opaque handle before the Dataspace records disappear.
        if (!MuiAreaCustomFontCore.CloseRuntime(ref platform, state, obj))
        {
            // Earlier cleanup has finished. Publish retry authority only after
            // the provider returns, so recursive disposal cannot resume it.
            if (MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, record,
                MuiHeadlessObjectField.Flags, out flagsField))
                platform.WriteUInt32(flagsField, 0,
                    platform.ReadUInt32(flagsField, 0) | ObjectAwaitingFontCleanup);
            return false;
        }
        MuiNotifyCore.RemoveAll(ref platform, state, record);
        MuiStoreCore.ClearAll(ref platform, state, record);
        // Store disposal may need the class-specific external pool attribute to
        // release pooled records and payloads. Retire generic attributes only
        // after the typed store lifetime has completed.
        FreeObjectAttributes(ref platform, record);
        if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
            out objectValue)) return false;
        var classRecord = objectValue.Class;
        if (!UnlinkObject(ref platform, state, record)) return false;
        if (disposeNative) platform.DisposeObject(obj);
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
        => TryFindObject(ref platform, state, obj, out var record) ? record : APTR.Null;

    internal static bool TryFindObject<TPlatform>(ref TPlatform platform, APTR state,
        APTR obj, out APTR record) where TPlatform : struct, IMuiHeadlessPlatform
    {
        record = APTR.Null;
        if (!MuiHeadlessMemory.Ensure(ref platform, state) || obj.IsNull)
            return false;
        if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
            out var stateValue)) return false;
        var current = stateValue.Objects;
        uint visited = 0;
        while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
        {
            if (!MuiHeadlessObjectCodec.TryRead(ref platform, current,
                out var objectValue))
                return false;
            if (objectValue.Boopsi.Raw == obj.Raw)
            {
                record = current;
                return true;
            }
            current = objectValue.Next;
        }
        return current.IsNull;
    }

    public static bool SetAttribute<TPlatform>(ref TPlatform platform, APTR state,
        APTR obj, uint attribute, uint value, bool notify)
        where TPlatform : struct, IMuiHeadlessPlatform
    {
        var record = FindObject(ref platform, state, obj);
        if (record.IsNull) return false;
        if (!SetRecordAttribute(ref platform, state, record, attribute, value,
            notify)) return false;
        // Direct raw setters are also used by construction-independent callers.
        // Keep the paired Area font choice synchronized even when the higher-level
        // layout setter is not the entry point.
        if (attribute == MuiCommonControlCore.Font ||
            attribute == MuiCommonControlCore.CustomFont)
        {
            if (!MuiAreaFontSelectionCore.Mark(ref platform, state, obj,
                attribute == MuiCommonControlCore.CustomFont && value == 0
                    ? MuiAreaFontSelectionKind.None
                    : attribute == MuiCommonControlCore.CustomFont
                        ? MuiAreaFontSelectionKind.CustomFont
                        : MuiAreaFontSelectionKind.Font,
                APTR.FromPointer(value))) return false;
            return MuiAreaCustomFontCore.Refresh(ref platform, state, obj);
        }
        return true;
    }

    // Apply one BOOPSI OM_SET TagItem list without copying it into host state.
    // MUIA_NoNotify is a setting-only control tag: when TRUE appears anywhere in
    // this operation, all effective attribute writes in the same list are made
    // without dispatching notifications. The control tag itself is never stored
    // as an object attribute. The list walk and its TAG_* control records stay
    // guest-resident and are decoded through MuiAslTagItemRecord.
    internal static bool SetAttributes<TPlatform>(ref TPlatform platform,
        APTR state, APTR obj, APTR tags)
        where TPlatform : struct, IMuiHeadlessPlatform
    {
        var record = FindObject(ref platform, state, obj);
        if (record.IsNull) return false;
        if (!TryReadNotificationSettings(ref platform, tags, out var noNotify,
            out var noNotifyMethod))
            return false;
        if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
            out var stateValue)) return false;
        var previousNoNotifyMethod = stateValue.NotifySuppressionMethod;
        if (noNotifyMethod != 0)
        {
            // The named NotifySuppressionMethod state field carries the
            // operation-local MUIA_NoNotifyMethod;
            // it is restored before returning and is never object state.
            stateValue.NotifySuppressionMethod = noNotifyMethod;
            if (!MuiHeadlessStateCodec.Write(ref platform, state, stateValue))
                return false;
        }
        var applied = ApplyTagList(ref platform, state, record, tags,
            !noNotify, true);
        if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
            out stateValue)) return false;
        stateValue.NotifySuppressionMethod = previousNoNotifyMethod;
        if (!MuiHeadlessStateCodec.Write(ref platform, state, stateValue))
            return false;
        return applied;
    }

    public static bool GetAttribute<TPlatform>(ref TPlatform platform, APTR state,
        APTR obj, uint attribute, out uint value)
        where TPlatform : struct, IMuiHeadlessPlatform
    {
        value = 0;
        var record = FindObject(ref platform, state, obj);
        if (record.IsNull) return false;
        // AutoLock/CopyKeys are MorphOS [I..] store policies. Keep them out of
        // OM_GET once a concrete store class is known; unknown compatibility
        // objects retain the generic raw-attribute behavior used by legacy callers.
        if (MuiStorePolicyCore.IsGetterlessForKnownStore(attribute,
            MuiCommonControlCore.Classify(ref platform, state, obj))) return false;
        // A Listview owns a named List child. Resolve the public List attribute
        // family from that typed child before consulting the parent's raw metadata;
        // otherwise a stale compatibility scalar on the composite can mask the
        // authoritative List state. Private Listview policy/state attributes stay
        // on the existing class-specific path below.
        var collectionClass = MuiListCore.Classify(ref platform, state, obj);
        if (collectionClass == MuiCollectionClass.Listview &&
            MuiListviewCore.TryGetChildRelationAttribute(ref platform, state, obj,
                attribute, out value)) return true;
        if (collectionClass == MuiCollectionClass.Listview &&
            MuiListCore.IsPublicGetterAttribute(attribute) &&
            MuiListviewCore.TryGetForwardedPublicAttribute(ref platform, state, obj,
                attribute, out value)) return true;
        if (MuiObjectMetadataCore.TryGet(ref platform, state, obj, attribute,
            out value)) return true;
        if (MuiHelpStateCore.IsAttribute(attribute))
        {
            if (!MuiHelpStateCore.TryReadState(ref platform, state, obj,
                out var help)) return false;
            value = attribute == MuiHelpStateCore.HelpNode ? help.Node.Raw :
                unchecked((uint)help.Line);
            return true;
        }
        // Collection classes own their public projections, including the
        // Listview DragType policy field. Input, MultiSelect, and ScrollerPos are
        // construction-only [I..] attributes: reject them before the generic raw
        // attribute list can accidentally turn them into getters.
        if (collectionClass == MuiCollectionClass.Listview &&
            MuiListviewCore.IsInitializeOnlyAttribute(attribute)) return false;
        if (collectionClass == MuiCollectionClass.Listview &&
            MuiListviewCore.IsGettableInteractionPolicyAttribute(attribute) &&
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
        if (MuiListtreeCore.IsListtree(ref platform, state, obj))
        {
            if (MuiListtreeCore.IsRuntimeSetOnlyAttribute(attribute)) return false;
            if (MuiListtreeCore.IsPublicGetterAttribute(attribute) &&
                MuiListtreeCore.GetAttribute(ref platform, state, obj, attribute,
                    out value)) return true;
        }
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
        if (MuiRegisterCore.TryGetAttribute(ref platform, state, obj, attribute,
            out value)) return true;
        if (MuiSelectgroupCore.TryGetAttribute(ref platform, state, obj, attribute,
            out value)) return true;
        if (MuiScrollgroupCore.TryGetAttribute(ref platform, state, obj, attribute,
            out value)) return true;
        if (MuiVirtgroupCore.TryGetAttribute(ref platform, state, obj, attribute,
            out value)) return true;
        if (MuiListCore.TryGetAttribute(ref platform, state, obj, attribute,
            out value)) return true;
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
        APTR state, APTR record, uint attribute, uint value, bool notify,
        bool routeCollectionRuntime = false)
        where TPlatform : struct, IMuiHeadlessPlatform
    {
        // These two tags are valid only as operation-local OM_SET controls;
        // direct MUIM_Set/SetAttribute calls must never turn them into object
        // attributes.
        if (attribute == NoNotifyAttribute ||
            attribute == NoNotifyMethodAttribute) return false;
        if (MuiStorePolicyCore.IsPolicyAttribute(attribute))
        {
            if (!MuiStorePolicyCore.TryGetPolicyKind(attribute,
                out var policy)) return false;
            if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
                out var policyObject) || !MuiStorePolicyCore.IsClassCompatible(
                ref platform, state, policyObject.Boopsi, policy)) return false;
        }
        // Construction tags enter this generic BOOPSI setter directly, before
        // class-aware interactive setters can validate caller-owned strings. Keep
        // the public STRPTR admission rule at the shared ABI boundary so malformed
        // constructor values cannot be published or copied as empty strings.
        var copiedCStringMaximum = MuiCommonControlCore.CopiedCStringMaximum(
            attribute);
        if (copiedCStringMaximum != 0 && value != 0 &&
            !MuiCommonControlCore.IsValidGuestCStringPointer(ref platform,
                APTR.FromPointer(value), copiedCStringMaximum)) return false;
        // Listview is a composite: its interaction policy lives in a named
        // guest record and its ordinary List attributes are owned by the child
        // List.  Generic BOOPSI OM_SET must therefore enter the same typed
        // runtime setter as MUIM_Set instead of leaving a stale raw scalar on
        // the Listview record.  Keep the admission test narrow so unrelated
        // Area attributes continue through the normal object store.
        if (routeCollectionRuntime && IsObjectInitialized(ref platform, record) &&
            MuiHeadlessObjectCodec.TryRead(ref platform, record,
            out var collectionObject) &&
            MuiListCore.ClassifyRecord(ref platform, collectionObject.Class) ==
                MuiCollectionClass.Listview &&
            (MuiListviewCore.IsPublicAttribute(attribute) ||
                MuiListCore.IsPublicGetterAttribute(attribute)))
            return MuiListviewCore.SetRuntimeAttribute(ref platform, state,
                collectionObject.Boopsi, attribute, value, notify);
        if (MuiHelpStateCore.IsAttribute(attribute))
            return MuiHelpStateCore.Set(ref platform, state, record, attribute,
                value, notify);
        // Listtree owns named policy and presentation records for its public
        // attributes. Route the complete class-gated public set here; the
        // Listtree core writes the raw attribute record directly to avoid
        // re-entering this seam.
        if (MuiHeadlessObjectCodec.TryRead(ref platform, record,
            out var listtreeObject) &&
            MuiListtreeCore.IsListtree(ref platform, state,
                listtreeObject.Boopsi) &&
            MuiListtreeCore.IsPublicSetAttribute(attribute))
            return MuiListtreeCore.SetAttribute(ref platform, state,
                listtreeObject.Boopsi, attribute, value, notify);
        var handled = false;
        if (MuiApplicationWindowCore.TrySet(ref platform, state, record, attribute,
            value, notify, out handled) && handled) return true;
        if (handled) return false;
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
        if (MuiRegisterCore.TrySet(ref platform, state, record, attribute, value,
            notify, out handled) && handled) return true;
        if (handled) return false;
        if (MuiSelectgroupCore.TrySet(ref platform, state, record, attribute,
            value, notify, out handled) && handled) return true;
        if (handled) return false;
        if (MuiScrollgroupCore.TrySet(ref platform, state, record, attribute,
            value, notify, out handled) && handled) return true;
        if (handled) return false;
        if (MuiVirtgroupCore.TrySet(ref platform, state, record, attribute,
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

    // Raw attribute view including the guest-store generation.  The generation
    // is used only to reconstruct MorphOS last-writer precedence for paired
    // selectors such as MUIA_Font and MUIA_CustomFont; callers still receive the
    // named semantic state rather than depending on the private Attribute node.
    internal static bool GetRawAttributeGeneration<TPlatform>(
        ref TPlatform platform, APTR state, APTR obj, uint attribute,
        out uint value, out uint generation)
        where TPlatform : struct, IMuiHeadlessPlatform
    {
        value = 0;
        generation = 0;
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
        if (item.IsNull || !MuiHeadlessAttributeCodec.TryRead(ref platform, item,
            out var attributeValue)) return false;
        value = attributeValue.Value;
        generation = attributeValue.Generation;
        return true;
    }

    private static bool ApplyTags<TPlatform>(ref TPlatform platform, APTR state,
        APTR record, APTR tags) where TPlatform : struct, IMuiHeadlessPlatform
    {
        // Construction tags are never notification-producing, but the same
        // setting-only control tag must not become a raw persistent attribute.
        if (!TryReadNotificationSettings(ref platform, tags, out _, out _))
            return false;
        return ApplyTagList(ref platform, state, record, tags, false, false);
    }

    private static bool TryReadNotificationSettings<TPlatform>(
        ref TPlatform platform, APTR tags, out bool noNotify,
        out uint noNotifyMethod)
        where TPlatform : struct, IMuiHeadlessPlatform
    {
        noNotify = false;
        noNotifyMethod = 0;
        var cursor = default(MuiAslTagItemCursor);
        cursor.Base = tags;
        uint visited = 0;
        while (cursor.Base.IsNotNull && visited++ <
            MuiHeadlessLayout.MaximumTraversal)
        {
            if (!MuiAslTagItemVectorCodec.TryRead(ref platform, cursor,
                out var item)) return false;
            var tag = item.Tag;
            if (tag == MuiAslTagListCore.TagDone) return true;
            if (tag == MuiAslTagListCore.TagIgnore)
            {
                if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
                    return false;
                continue;
            }
            if (tag == MuiAslTagListCore.TagMore)
            {
                if (item.Data == 0) return true;
                cursor.Base = APTR.FromPointer(item.Data);
                cursor.Index = 0;
                continue;
            }
            if (tag == MuiAslTagListCore.TagSkip)
            {
                if (item.Data == uint.MaxValue ||
                    !MuiAslTagItemVectorCodec.TryAdvance(ref cursor,
                        item.Data + 1u)) return false;
                continue;
            }
            if (tag == NoNotifyAttribute &&
                item.Data != 0) noNotify = true;
            if (tag == NoNotifyMethodAttribute)
                noNotifyMethod = item.Data;
            if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
                return false;
        }
        return cursor.Base.IsNull;
    }

    private static bool ApplyTagList<TPlatform>(ref TPlatform platform,
        APTR state, APTR record, APTR tags, bool notify,
        bool routeCollectionRuntime)
        where TPlatform : struct, IMuiHeadlessPlatform
    {
        var cursor = default(MuiAslTagItemCursor);
        cursor.Base = tags;
        uint visited = 0;
        while (cursor.Base.IsNotNull && visited++ <
            MuiHeadlessLayout.MaximumTraversal)
        {
            if (!MuiAslTagItemVectorCodec.TryRead(ref platform, cursor,
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
                if (data == 0) return true;
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
            if (tag == NoNotifyAttribute || tag == NoNotifyMethodAttribute)
            {
                if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
                    return false;
                continue;
            }
            if (!SetRecordAttribute(ref platform, state, record, tag, data, notify,
                routeCollectionRuntime))
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
        where TPlatform : struct, IMuiGuestMemory =>
        MuiHeadlessClassCodec.TryRead(ref platform, record,
            out var classValue) && classValue.Boopsi.IsNotNull;

    private static bool UnlinkClass<TPlatform>(ref TPlatform platform, APTR state,
        APTR target) where TPlatform : struct, IMuiGuestMemory
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
                    if (!MuiHeadlessStateMemoryCodec.TryGetAddress(ref platform, state,
                        MuiHeadlessStateField.Objects, out var head)) return false;
                    platform.WriteUInt32(head, 0, objectValue.Next.Raw);
                }
                else
                {
                    if (!MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, previous,
                        MuiHeadlessObjectField.Next, out var link)) return false;
                    platform.WriteUInt32(link, 0, objectValue.Next.Raw);
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

}
