using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.LoadResourceNativeRoot;

/// <summary>Qualification-only driver with worker-owned registry/catalog state and supplied library leases.</summary>
public static class NativeWorkbench31LoadResourceActionsProbe
{
    [M68kEntryPoint]
    public static int Main(int missingLocale, CONST_STRPTR contextPointer)
    {
        var context = contextPointer.Address;
        Utility.UtilityLibraryBase = APTR.FromPointer(0x9000);
        Graphics.GraphicsLibraryBase = APTR.FromPointer(0xa000);
        Locale.LocaleLibraryBase = missingLocale != 0 ? APTR.Null : APTR.FromPointer(0xb000);
        NativeWorkbench31LoadResourceRegistry.Initialize(context);
        if (!NativeWorkbench31LoadResourceLoadSeg.TryInstall())
            return 0x7f01;
        var name = CString.FromPointer(context.Raw + 64);
        if (APTR.ReadUInt32(context, 44) != 0 &&
            !NativeWorkbench31LoadResourceRegistry.TryAdd(context, name, 0, APTR.FromPointer(0x18000)))
            return 0x7f02;
        var result = NativeWorkbench31LoadResourceActions.Load(context, name,
            APTR.ReadUInt32(context, 16) != 0,
            APTR.FromPointer(APTR.ReadUInt32(context, 20)));
        APTR.WriteUInt32(context, 12, unchecked((uint)result));
        APTR.WriteUInt32(context, 24, NativeWorkbench31LoadResourceRegistry.IsEmpty(context) ? 1u : 0u);
        var task = Exec.FindTask(CString.FromPointer(0));
        var state = APTR.FromPointer(APTR.ReadUInt32(task, ExecLayout.Task.UserData));
        APTR.WriteUInt32(context, 28, APTR.ReadUInt32(state, 0) == state.Raw + 4 ? 1u : 0u);
        if (APTR.ReadUInt32(context, 36) != 0)
            APTR.WriteUInt32(context, 40, Invoke(
                APTR.ExportAddress(NativeWorkbench31LoadResourceLoadSeg.HookExport), DOS.DOSLibraryBase, name));
        while (!NativeWorkbench31LoadResourceRegistry.IsEmpty(context))
            NativeWorkbench31LoadResourceRegistry.CloseAndRemove(
                APTR.FromPointer(APTR.ReadUInt32(context, 0)));
        NativeWorkbench31LoadResourceLoadSeg.TryRemove(BPTR.FromRaw(APTR.ReadUInt32(context, 32)));
        return NativeWorkbench31LoadResourceLoadSeg.TryUninstallWhenIdle() ? result : 0x7f03;
    }

    [AmigaIndirectCall(M68kRegister.A3)]
    [return: M68kRegister(M68kRegister.D0)]
    private static extern uint Invoke(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR dosBase,
        [M68kRegister(M68kRegister.D1)] CString name);
}
