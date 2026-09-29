using Amiga;
using CopperSharp.Compiler;
using CopperStart.Intuition;
using CopperSharp.Sdk.Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster.ExecIntegration;

// Native subsystem fixture: owns the base/vector allocation and binds actual
// production BOOPSI entrypoints. Not a published intuition.library lifecycle.
// Rootclass, class/object storage and all cleanup use real installed Exec.
public static class BoopsiBootstrap
{
    public const string CreateExport = "copperos.mui.test.boopsi-create";
    public const string DestroyExport = "copperos.mui.test.boopsi-destroy";
    public const string AvailableExport = "copperos.mui.test.boopsi-available";
    public const uint AllocationBytes = IntuitionBoopsiOwnedEntrypoints.NegativeBytes +
        IntuitionBoopsiOwnedEntrypoints.PositiveBytes;

    [M68kExport(CreateExport)]
    [return: M68kRegister(M68kRegister.D0)]
    public static APTR Create([M68kRegister(M68kRegister.A0)] APTR bindingsAddress)
    {
        var memory = new CopperStart.Exec.CopperSharpRomMemoryPlatform(ExecBootstrap.SysBase);
        var bindings = BoopsiBootstrapBindingsCodec.Read(ref memory, bindingsAddress);
        var allocation = APTR.FromPointer(global::Amiga.Exec.AllocMem(AllocationBytes,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
        if (allocation.IsNull) return APTR.Null;
        var library = APTR.FromPointer(allocation.Raw + IntuitionBoopsiOwnedEntrypoints.NegativeBytes);
        Library header = default;
        header.Node.Type = (byte)NodeType.Library;
        header.PositiveSize = IntuitionBoopsiOwnedEntrypoints.PositiveBytes;
        header.NegativeSize = IntuitionBoopsiOwnedEntrypoints.NegativeBytes;
        ExecLibraryCodec.Write(ref memory, library, header);
        if (BoopsiStorageCall.Invoke(bindings.Initialize, library) == 0)
        {
            global::Amiga.Exec.FreeMem(allocation.Raw, AllocationBytes);
            return APTR.Null;
        }
        Install(library, IntuitionLvo.MakeClass, bindings.MakeClass);
        Install(library, IntuitionLvo.FreeClass, bindings.FreeClass);
        Install(library, IntuitionLvo.AddClass, bindings.AddClass);
        Install(library, IntuitionLvo.RemoveClass, bindings.RemoveClass);
        Install(library, IntuitionLvo.NewObjectA, bindings.NewObject);
        Install(library, IntuitionLvo.DisposeObject, bindings.DisposeObject);
        return library;
    }

    [M68kExport(DestroyExport)]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Destroy([M68kRegister(M68kRegister.A0)] APTR library,
        [M68kRegister(M68kRegister.A1)] APTR destroyEntry)
    {
        if (BoopsiStorageCall.Invoke(destroyEntry, library) == 0) return 0;
        global::Amiga.Exec.FreeMem(library.Raw - IntuitionBoopsiOwnedEntrypoints.NegativeBytes,
            AllocationBytes);
        return 1;
    }

    private static void Install(APTR library, short lvo, APTR entry)
    {
        // SetFunction's real implementation writes the absolute JMP and updates
        // the header/checksum. Uninstalled vectors remain outside this fixture's contract.
        global::Amiga.Exec.SetFunction(library, lvo, entry);
    }

    [M68kExport(AvailableExport)]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Available() => global::Amiga.Exec.AvailMem(global::Amiga.Exec.MemoryFlags.Public);
}

internal static class BoopsiStorageCall
{
    [AmigaIndirectCall(M68kRegister.A3)]
    [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Invoke([M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR library);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct BoopsiBootstrapBindings
{
    internal const uint Size = 28;
    internal APTR Initialize, MakeClass, FreeClass, AddClass, RemoveClass, NewObject, DisposeObject;
}

// Passive binding metadata only. All target addresses refer to actual exported
// production adapters in a separately compiled Intuition subsystem image.
internal static class BoopsiBootstrapBindingsCodec
{
    internal const uint Address = 0x00038000;
    internal static BoopsiBootstrapBindings Read<T>(ref T memory, APTR address) where T : struct, IAmigaGuestMemory => new()
    {
        Initialize = APTR.FromPointer(memory.ReadUInt32(address, 0)),
        MakeClass = APTR.FromPointer(memory.ReadUInt32(address, 4)),
        FreeClass = APTR.FromPointer(memory.ReadUInt32(address, 8)),
        AddClass = APTR.FromPointer(memory.ReadUInt32(address, 12)),
        RemoveClass = APTR.FromPointer(memory.ReadUInt32(address, 16)),
        NewObject = APTR.FromPointer(memory.ReadUInt32(address, 20)),
        DisposeObject = APTR.FromPointer(memory.ReadUInt32(address, 24)),
    };
    internal static void Write(NativeExecBus bus, in BoopsiBootstrapBindings value)
    {
        bus.WriteLong(Address, value.Initialize.Raw);
        bus.WriteLong(Address + 4, value.MakeClass.Raw);
        bus.WriteLong(Address + 8, value.FreeClass.Raw);
        bus.WriteLong(Address + 12, value.AddClass.Raw);
        bus.WriteLong(Address + 16, value.RemoveClass.Raw);
        bus.WriteLong(Address + 20, value.NewObject.Raw);
        bus.WriteLong(Address + 24, value.DisposeObject.Raw);
    }
}
