/* SPDX-License-Identifier: MIT */
using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeBuiltinInstance
{
    internal const ushort Size = 8;
    internal const int ReferencesOffset = 0;
    internal const int PinsOffset = 4;
    internal uint References;
    internal uint Pins;
}

// Production adapter for the existing guest-resident built-in class cores.
// Optional OS services that are not implemented fail closed; no fixture arena,
// host callback, synthetic window or fake filesystem result is used here.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal partial struct MuiNativeBuiltinPlatform : IMuiLayoutPlatform, IMuiClassicMenuPlatform, IMuiBuiltinWindowCapability
{
    private APTR _context;
    // Keep the generic ABI at four bytes. During layout the low bit denotes
    // an owned frame containing the canonical context plus transient state.
    internal APTR Context
    {
        get => (_context.Raw & 1) == 0 ? _context :
            APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(_context.Raw & ~1u), 0));
        set => _context = value;
    }
    private APTR MinMaxPass => (_context.Raw & 1) == 0 ? APTR.Null :
        APTR.FromPointer((_context.Raw & ~1u) + 4);
    public bool TryReadMuiMinMax(APTR obj, out MuiMinMaxValues values) =>
        MuiNativeBuiltinMinMax.TryRead(ref this, MinMaxPass, Registry, obj, out values);
    internal bool BeginMinMax(APTR root)
    {
        var context = Context;
        var frame = Allocate(4 + MuiNativeBuiltinMinMax.PassSize, MuiHeadlessLayout.AllocationFlags);
        if (frame.IsNull) return false;
        WriteUInt32(frame, 0, context.Raw);
        _context = APTR.FromPointer(frame.Raw | 1u);
        if (MuiNativeBuiltinMinMax.Begin(ref this, MinMaxPass, Registry, root)) return true;
        EndMinMax(); return false;
    }
    internal void EndMinMax()
    {
        if ((_context.Raw & 1) == 0) return;
        var frame = APTR.FromPointer(_context.Raw & ~1u);
        MuiNativeBuiltinMinMax.End(ref this, MinMaxPass);
        var context = APTR.FromPointer(ReadUInt32(frame));
        _context = context;
        Free(frame, 4 + MuiNativeBuiltinMinMax.PassSize);
    }
    internal MuiNativeClassPlatform Native => new MuiNativeClassPlatform { Context = Context };
    internal APTR Registry => MuiNativeClassOwnerCodec.TryGetRegistryAddress(ref this, Context, out var value) ? value : APTR.Null;
    public bool IsMenustripObject(APTR value) =>
        MuiMenuSpecialistCore.Classify(ref this, Registry, value) == MuiMenuSpecialistClass.Menustrip;
    public APTR CreateClassicMenustrip(APTR newMenu)
    {
        var native = Native;
        if (!MuiNativeClassOwnerCodec.TryRead(ref this, Context, out var owner) ||
            !MuiNativeClassOwnerCodec.TryGetServiceAddress(ref this, Context, out var service) ||
            !MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref this, Context, out var objects)) return APTR.Null;
        var parameters = Allocate(8, MuiHeadlessLayout.AllocationFlags);
        if (parameters.IsNull) return APTR.Null;
        WriteUInt32(parameters, 0, newMenu.Raw);
        var strip = MuiMakeObjectServiceCore.MakeNativeObjectA(ref native, service,
            owner.Context.OwnerRoot, objects, MuiMakeObjectServiceCore.MUIO_MenustripNM, parameters);
        Free(parameters, 8);
        return strip;
    }
    public void DisposeClassicMenustrip(APTR strip)
    {
        var native = Native;
        // Construction may already have adopted and destroyed the generated
        // strip. Never dispose a stale pointer or steal a partial owner's child.
        if (MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref this, Context, out var objects) &&
            MuiNativePublicObjectCore.TryFindBinding(ref native, objects, native.OwnerRoot, strip, out _) &&
            MuiHeadlessObjectCore.TryFindObject(ref this, Registry, strip, out var record) &&
            (record.IsNull || MuiHeadlessObjectCodec.TryRead(ref this, record, out var value) && value.Parent.IsNull))
            DisposeObject(strip);
    }
    public bool IsMapped(APTR a, uint n) => default(MuiNativeClassMemory).IsMapped(a, n);
    public byte ReadUInt8(APTR a, int o = 0) => APTR.ReadUInt8(a, o);
    public ushort ReadUInt16(APTR a, int o = 0) => APTR.ReadUInt16(a, o);
    public uint ReadUInt32(APTR a, int o = 0) => APTR.ReadUInt32(a, o);
    public void WriteUInt8(APTR a, int o, byte v) => APTR.WriteUInt8(a, o, v);
    public void WriteUInt16(APTR a, int o, ushort v) => APTR.WriteUInt16(a, o, v);
    public void WriteUInt32(APTR a, int o, uint v) => APTR.WriteUInt32(a, o, v);
    public void Clear(APTR a, uint n) => default(MuiNativeClassMemory).Clear(a, n);
    public void Copy(APTR s, APTR d, uint n) => default(MuiNativeClassMemory).Copy(s, d, n);
    public APTR Allocate(uint n, uint flags) => Exec.AllocMem(n, (Exec.MemoryFlags)flags);
    public void Free(APTR a, uint n) => Exec.FreeMem(a, n);
    public APTR CreatePool(uint flags, uint puddle, uint threshold) => Exec.CreatePool(flags, puddle, threshold);
    public void DeletePool(APTR pool) => Exec.DeletePool(pool);
    public APTR AllocPooled(APTR pool, uint n) => Exec.AllocPooled(pool, n);
    public void FreePooled(APTR pool, APTR a, uint n) => Exec.FreePooled(pool, a, n);
    public APTR MakeClass(APTR name, APTR super, ushort size, APTR dispatcher)
    {
        var native = Native;
        var cls = native.MakeClass(name, super, size, dispatcher);
        if (cls.IsNotNull)
        {
            var value = BOOPSIGuestCodec.ReadClass(ref this, cls);
            value.cl_Dispatcher.Data = Context;
            BOOPSIGuestCodec.WriteClass(ref this, cls, value);
        }
        return cls;
    }
    public bool AddClass(APTR cls) => Native.AddClass(cls);
    public bool RemoveClass(APTR cls) => Native.RemoveClass(cls);
    public bool FreeClass(APTR cls) => Native.FreeClass(cls);
    public APTR NewObject(APTR cls, APTR tags) => Native.NewObject(cls, tags);
    public void DisposeObject(APTR obj)
    {
        var native = Native;
        if (MuiNativeClassOwnerCodec.TryRead(ref this, Context, out var owner) &&
            MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref this, Context, out var objects) &&
            MuiNativePublicObjectCore.TryFindBinding(ref native, objects, native.OwnerRoot, obj, out var binding))
        {
            ReleaseBuiltinWindow(obj);
            // Family cleanup has already retired the headless child link. Drop
            // its parallel native relationship before the public disposal seam.
            if (binding.Parent.IsNotNull &&
                MuiHeadlessObjectCore.TryFindObject(ref this, Registry, obj, out var record) && record.IsNull)
                if (!MuiNativePublicObjectCore.ClearParent(ref native, native.OwnerRoot, objects, binding.Parent, obj)) return;
            MuiNativePublicClassGateway.DisposeObject(owner.LibraryBase, obj);
        }
        else native.DisposeObject(obj);
    }
    public uint DoMethod(APTR obj, APTR message)
    {
        MuiNativeBuiltinMinMax.Invalidate(ref this, MinMaxPass);
        return Native.DoMethod(obj, message);
    }
    public uint CoerceMethod(APTR cls, APTR obj, APTR message) => Native.CoerceMethod(cls, obj, message);
    public uint DoSuperMethod(APTR cls, APTR obj, APTR message)
    {
        var super = BOOPSIGuestCodec.ReadClass(ref this, cls).cl_Super;
        if (super.IsNull || message.IsNull) return 0;
        var value = BOOPSIGuestCodec.ReadClass(ref this, super);
        if (!MuiNativeClassOwnerCodec.TryRead(ref this, Context, out var owner)) return 0;
        return MuiNativeRedrawCalls.Dispatch(value.cl_Dispatcher.Entry, super, obj, message, owner.LibraryBase);
    }
    public APTR InstanceData(APTR cls, APTR obj) => APTR.FromPointer(obj.Raw + BOOPSIGuestCodec.ReadClass(ref this, cls).cl_InstOffset);
    internal APTR References(APTR obj)
    {
        var record = MuiHeadlessObjectCore.FindClassByName(ref this, Registry, MuiNativeBuiltinClasses.Name(0));
        return record.IsNull ? APTR.Null : InstanceData(MuiHeadlessObjectCore.ClassPointer(ref this, record), obj);
    }
    public bool RetainObject(APTR obj)
    {
        if (!MuiHeadlessObjectCore.TryFindObject(ref this, Registry, obj, out var record) || record.IsNull) return false;
        var storage = References(obj);
        if (storage.IsNull) return false;
        // Headless Family construction pins are separate from public BOOPSI
        // retain counts: adopting a child transfers its classic MUI ownership.
        var count = ReadUInt32(storage, MuiNativeBuiltinInstance.PinsOffset);
        if (count == uint.MaxValue) return false;
        WriteUInt32(storage, MuiNativeBuiltinInstance.PinsOffset, count + 1);
        return true;
    }
    public bool ReleaseObject(APTR obj)
    {
        var storage = References(obj);
        if (storage.IsNull) return false;
        var count = ReadUInt32(storage, MuiNativeBuiltinInstance.PinsOffset);
        if (count == 0) return false;
        WriteUInt32(storage, MuiNativeBuiltinInstance.PinsOffset, count - 1);
        return count == 1;
    }
    public uint InvokeHook(APTR hook, APTR obj, APTR message)
    {
        MuiNativeBuiltinMinMax.Invalidate(ref this, MinMaxPass);
        return MuiNativeClassOwnerCodec.TryRead(ref this, Context, out var owner) && owner.UtilityBase.IsNotNull
            ? MuiNativeUtilityCalls.CallHookPkt(owner.UtilityBase, hook, obj, message) : 0;
    }
    public uint CurrentTaskToken() => Exec.FindTask(CString.FromPointer(0)).Raw;
    private uint Dos(short lvo, uint d1, uint d2, uint d3)
    {
        if (!MuiNativeClassOwnerCodec.TryRead(ref this, Context, out var owner) || owner.DosBase.IsNull) return 0;
        return InvokeDos(APTR.FromPointer(unchecked(owner.DosBase.Raw + (uint)lvo)), owner.DosBase, d1, d2, d3);
    }
    public APTR Open(APTR name, int mode) => APTR.FromPointer(Dos(-30, name.Raw, unchecked((uint)mode), 0));
    public int Close(APTR handle) => unchecked((int)Dos(-36, handle.Raw, 0, 0));
    public int Read(APTR handle, APTR buffer, uint length) => unchecked((int)Dos(-42, handle.Raw, buffer.Raw, length));
    public int Write(APTR handle, APTR buffer, uint length) => unchecked((int)Dos(-48, handle.Raw, buffer.Raw, length));
    public int IoErr() => unchecked((int)Dos(-132, 0, 0, 0));
    public int DirectoryScan(APTR path) => -1;
    public bool DirectoryEntry(APTR path, int index, APTR storage) => false;
    public int VolumeScan() => -1;
    public bool VolumeEntry(int index, APTR storage) => false;
    public int DirectoryRename(APTR path, APTR from, APTR to) => 209;
    public int DirectorySetComment(APTR path, APTR name, APTR comment) => 209;
    public int DirectorySetProtection(APTR path, APTR name, uint mask) => 209;
    public int DirectoryError() => 209;
    public bool ExportMuiObject(APTR obj, APTR dataspace, uint id) => false;
    public bool ImportMuiObject(APTR obj, APTR dataspace, uint id) => false;
    public bool GetMuiConfigItem(APTR obj, uint id, out uint value) { value = 0; return false; }
    public APTR OpenMuiCustomFont(ref MuiCustomFontOpenRequest request) => APTR.Null;
    public bool CloseMuiCustomFont(APTR font) => false;
    public bool TryGetMuiCustomFontMetrics(APTR font, out MuiCustomFontMetrics metrics) { metrics = default; return false; }
    public bool CheckMuiShortHelp(ref MuiShortHelpCheckSample sample) => false;
    public bool CreateMuiShortHelp(ref MuiShortHelpCreateSample sample) => false;
    public bool DeleteMuiShortHelp(ref MuiShortHelpDeleteSample sample) => false;
    public bool CreateMuiBubble(ref MuiBubbleCreateSample sample) => false;
    public bool DeleteMuiBubble(ref MuiBubbleDeleteSample sample) => false;
    public bool AddMuiContextMenu(ref MuiContextMenuAddSample sample) => false;
    public bool HandleMuiContextMenuChoice(ref MuiContextMenuChoiceSample sample) => false;
    public bool CreateMuiDragImage(ref MuiDragImageCreateSample sample) => false;
    public bool DeleteMuiDragImage(ref MuiDragImageDeleteSample sample) => false;
    public bool CaptureMuiPointer(ref MuiPointerCaptureSample sample) => false;
    public bool ReleaseMuiPointer(ref MuiPointerCaptureSample sample) => false;
    public bool RouteMuiDrag(ref MuiDragRouteSample sample) => false;
    [AmigaIndirectCall(M68kRegister.A3)]
    [return: M68kRegister(M68kRegister.D0)]
    private static extern uint InvokeDos([M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR library, [M68kRegister(M68kRegister.D1)] uint d1,
        [M68kRegister(M68kRegister.D2)] uint d2, [M68kRegister(M68kRegister.D3)] uint d3);
}
