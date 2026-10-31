/* SPDX-License-Identifier: MIT */
using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.MuiMaster;

public static class MuiNativeBuiltinClasses
{
    internal const int Count = 16;
    public const string DispatchExport = "copperos.mui.builtin.dispatch";
    internal static APTR Name(int index) => APTR.FromPointer(CString.ToUInt32(CString.FromLiteral(index switch
    {
        0 => "Notify.mui", 1 => "Family.mui", 2 => "Area.mui", 3 => "Group.mui",
        4 => "Rectangle.mui", 5 => "Text.mui", 6 => "List.mui", 7 => "Listview.mui",
        8 => "Window.mui", 9 => "Application.mui", 10 => "Menustrip.mui",
        11 => "Menu.mui", 12 => "Menuitem.mui", 13 => "String.mui", 14 => "Image.mui", _ => "Cycle.mui",
    })));
    internal static int Parent(int index) => index switch
    {
        0 => -1, 1 or 2 or 8 or 12 => 0, 3 or 4 or 5 or 6 or 13 or 14 => 2,
        7 or 15 => 3, _ => 1,
    };
    internal static bool Ensure(ref MuiNativeClassPlatform native, APTR requested)
    {
        var p = new MuiNativeBuiltinPlatform { Context = native.Context };
        var known = false;
        for (var i = 0; i < Count; i++)
            if (CStringCodec.TryEquals(ref p, Name(i), requested, 64, out var equal) && equal) known = true;
        if (!known) return true; // Preserve external custom-class loading.
        if (!MuiHeadlessStateCodec.TryRead(ref p, p.Registry, out var before)) return false;
        for (var i = 0; i < Count; i++)
        {
            if (MuiHeadlessObjectCore.FindClassByName(ref p, p.Registry, Name(i)).IsNotNull) continue;
            var parent = Parent(i);
            var super = APTR.Null;
            if (parent >= 0)
            {
                var record = MuiHeadlessObjectCore.FindClassByName(ref p, p.Registry, Name(parent));
                super = MuiHeadlessObjectCore.ClassPointer(ref p, record);
                if (super.IsNull) { Rollback(ref p, before.Classes); return false; }
            }
            if (MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, p.Registry, Name(i), super,
                i == 0 ? MuiNativeBuiltinInstance.Size : (ushort)0, APTR.ExportAddress(DispatchExport)).IsNull)
            { Rollback(ref p, before.Classes); return false; }
        }
        return true;
    }
    private static void Rollback(ref MuiNativeBuiltinPlatform p, APTR stop)
    {
        for (var i = 0; i < Count; i++)
        {
            if (!MuiHeadlessStateCodec.TryRead(ref p, p.Registry, out var state) || state.Classes == stop) return;
            if (!MuiHeadlessObjectCore.DeleteClass(ref p, p.Registry, state.Classes)) return;
        }
    }
    // Called while the library's service gate is still held. Classes are native
    // owned allocations, retired derived-first only after every lease/object
    // and custom subclass is gone. A failed native FreeClass keeps ownership.
    internal static void RetireIdle(ref MuiNativeServiceLease service)
    {
        var p = new MuiNativeBuiltinPlatform { Context = service.Operation.Owner };
        if (!MuiNativeClassOwnerCodec.TryRead(ref p, p.Context, out var owner) ||
            !MuiMasterPrivateRootCodec.TryRead(ref p, owner.Context.OwnerRoot, out var root) ||
            root.ExternalClassHead != 0 || root.ActiveCallbackDepth != 0 || owner.Service.Head.IsNotNull ||
            owner.Registry.Objects.IsNotNull || owner.PublicObjects.Head.IsNotNull) return;
        for (var i = 0; i < Count; i++)
        {
            if (!MuiHeadlessStateCodec.TryRead(ref p, p.Registry, out var state) || state.Classes.IsNull ||
                !MuiHeadlessClassCodec.TryRead(ref p, state.Classes, out var cls)) return;
            var native = BOOPSIGuestCodec.ReadClass(ref p, cls.Boopsi);
            if (native.cl_Dispatcher.Entry != APTR.ExportAddress(DispatchExport) ||
                native.cl_Dispatcher.Data != p.Context) return;
            if (!MuiHeadlessObjectCore.DeleteClass(ref p, p.Registry, state.Classes)) return;
        }
    }
    internal static APTR Find(ref MuiNativeBuiltinPlatform p, APTR cls)
    {
        if (!MuiHeadlessStateCodec.TryRead(ref p, p.Registry, out var state)) return APTR.Null;
        var scan = state.Classes;
        for (uint i = 0; scan.IsNotNull && i < MuiHeadlessLayout.MaximumTraversal; i++)
        {
            if (!MuiHeadlessClassCodec.TryRead(ref p, scan, out var value)) return APTR.Null;
            if (value.Boopsi == cls) return scan;
            scan = value.Next;
        }
        return APTR.Null;
    }
    [M68kExport(DispatchExport)]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Dispatch([M68kRegister(M68kRegister.A0)] APTR cls,
        [M68kRegister(M68kRegister.A2)] APTR obj, [M68kRegister(M68kRegister.A1)] APTR message,
        [M68kRegister(M68kRegister.A6)] APTR callerBase)
    {
        var memory = default(MuiNativeClassMemory);
        if (cls.IsNull || message.IsNull) return 0;
        var native = BOOPSIGuestCodec.ReadClass(ref memory, cls);
        var p = new MuiNativeBuiltinPlatform { Context = native.cl_Dispatcher.Data };
        if (!MuiNativeClassOwnerCodec.TryRead(ref p, p.Context, out var owner) ||
            !MuiNativeClassOwnerCore.TryReadAttached(ref p, owner.LibraryBase, owner.Context.OwnerRoot,
                out _, out var attached, out _) || attached != p.Context ||
            !MuiBoopsiMethodMessageCodec.TryReadMethodId(ref p, message, out var method)) return 0;
        Exec.Forbid();
        if (!MuiMasterPrivateRootCodec.TryRead(ref p, owner.Context.OwnerRoot, out var root) ||
            root.ActiveCallbackDepth == uint.MaxValue)
        { Exec.Permit(); return 0; }
        root.ActiveCallbackDepth++;
        var pinned = MuiMasterPrivateRootCodec.Write(ref p, owner.Context.OwnerRoot, root);
        Exec.Permit();
        if (!pinned) return 0;
        var answer = DispatchCore(ref p, cls, obj, message, method);
        Exec.Forbid();
        if (MuiMasterPrivateRootCodec.TryRead(ref p, owner.Context.OwnerRoot, out root) && root.ActiveCallbackDepth != 0)
        {
            root.ActiveCallbackDepth--;
            MuiMasterPrivateRootCodec.Write(ref p, owner.Context.OwnerRoot, root);
        }
        Exec.Permit();
        return answer;
    }
    private static uint DispatchCore(ref MuiNativeBuiltinPlatform p, APTR cls, APTR obj, APTR message, uint method)
    {
        var record = Find(ref p, cls);
        if (record.IsNull) return 0;
        var notify = MuiHeadlessObjectCore.FindClassByName(ref p, p.Registry, Name(0));
        if (method == BOOPSI.OM_NEW)
        {
            if (record != notify) return p.DoSuperMethod(cls, obj, message);
            var owning = Find(ref p, obj); // BOOPSI's constructor operand is the most-derived IClass.
            if (owning.IsNull || !MuiBoopsiOpSetMessageCodec.TryRead(ref p, message, out var packet)) return 0;
            // Reserve cleanup storage before constructing native state.
            var disposal = p.Allocate(4, (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
            if (disposal.IsNull) return 0;
            p.WriteUInt32(disposal, 0, BOOPSI.OM_DISPOSE);
            var result = APTR.FromPointer(p.DoSuperMethod(cls, obj, message));
            if (result.IsNull) { p.Free(disposal, 4); return 0; }
            p.WriteUInt32(p.InstanceData(cls, result), 0, 1);
            var initialized = MuiHeadlessObjectCore.AttachConstructedObjectForDispatcher(ref p,
                p.Registry, owning, result, packet.Attributes, out var ownership);
            if (initialized.IsNotNull &&
                (!MuiCommonControlCore.Construct(ref p, p.Registry, owning, result) ||
                 !MuiListCore.Construct(ref p, p.Registry, owning, result) ||
                 !MuiListviewCore.Construct(ref p, p.Registry, owning, result)))
                initialized = APTR.Null;
            if (initialized.IsNull &&
                (ownership == MuiObjectAttachmentOwnership.CallerOwned ||
                 ownership == MuiObjectAttachmentOwnership.Transferred &&
                 MuiHeadlessObjectCore.DisposeObjectState(ref p, p.Registry, result, false)))
                p.DoSuperMethod(cls, result, disposal);
            p.Free(disposal, 4);
            return initialized.Raw;
        }
        if (method == BOOPSI.OM_DISPOSE)
        {
            if (record != notify) return p.DoSuperMethod(cls, obj, message);
            // Family teardown may have retired the child's sidecar before
            // releasing its native object. Do not strand that root allocation.
            if (MuiHeadlessObjectCore.TryFindObject(ref p, p.Registry, obj, out var existing) && existing.IsNull)
                return p.DoSuperMethod(cls, obj, message);
            // An owning Family can enter native disposal after it has already
            // begun this child's headless cleanup. Complete only the native
            // superclass stage; re-entering the state destructor would reject
            // its different cleanup mode and strand the native allocation.
            if (existing.IsNotNull && MuiHeadlessObjectCodec.TryRead(ref p, existing, out var disposing) &&
                (disposing.Flags & (MuiHeadlessObjectCore.ObjectDisposing | 16u)) == MuiHeadlessObjectCore.ObjectDisposing)
                return p.DoSuperMethod(cls, obj, message);
            var references = p.References(obj);
            if (references.IsNotNull && p.ReadUInt32(references) > 1)
            { p.WriteUInt32(references, 0, p.ReadUInt32(references) - 1); return 0; }
            return MuiBoopsiBaseDisposalCore.TryDispose(ref p, p.Registry, record, obj, message, out var result) ? result : 0;
        }
        if (method == MuiNativeBoopsiMethodId.RetainCount)
        {
            var references = p.References(obj);
            return references.IsNull ? 0 : p.ReadUInt32(references);
        }
        if (method == BOOPSI.OM_GET)
        {
            if (!MuiExternalBoopsiOpGetMessageStructCodec.TryRead(ref p, message, out var get)) return 0;
            // Headless state records the requested Open attribute. Native
            // Window presentation is a separate capability; never report an
            // open window or manufacture a Window* without that presenter.
            if (record == MuiHeadlessObjectCore.FindClassByName(ref p, p.Registry, Name(8)) &&
                (get.Attribute == MuiWindowPublicCore.Open || get.Attribute == MuiWindowPublicCore.Window))
                return MuiGuestUlongStorageCodec.WriteValue(ref p, get.Storage, 0) ? 1u : 0u;
            if (!MuiHeadlessObjectCore.GetAttribute(ref p, p.Registry, obj, get.Attribute, out var value)) return 0;
            return MuiGuestUlongStorageCodec.WriteValue(ref p, get.Storage, value) ? 1u : 0u;
        }
        if (MuiListCore.Classify(ref p, p.Registry, obj) == MuiCollectionClass.List)
        {
            // State methods are independent of the layout/rendering capability.
            // Decode their public packets through the same declaration-ordered
            // codecs as the collection dispatcher, then use the real List core.
            if (method == 0x804254d5u && MuiCollectionAdvancedMessageCodec.TryReadInsertSingle(ref p, message, out var insert))
                return MuiListCore.InsertSingle(ref p, p.Registry, obj, APTR.FromPointer(insert.Entry), unchecked((int)insert.Position)) ? 1u : 0u;
            if (method == 0x8042ad89u) return MuiListCore.Clear(ref p, p.Registry, obj) ? 1u : 0u;
        }
        return MuiHeadlessDispatcher.Dispatch(ref p, p.Registry, obj, message);
    }
}
