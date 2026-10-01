using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.LoadResourceNativeRoot;

/// <summary>Executable qualification driver; not an installed command.</summary>
public static class NativeWorkbench31LoadResourceHookProbe
{
    [M68kEntryPoint]
    public static int Main(int scenario, CONST_STRPTR unused)
    {
        var installed = NativeWorkbench31LoadResourceLoadSeg.TryInstall();
        if (scenario >= 3 && scenario <= 5 || scenario == 9)
            return installed ? 101 : 0;
        if (!installed)
            return 102;

        if (scenario == 1)
        {
            if (NativeWorkbench31LoadResourceLoadSeg.TryInstall())
                return 103;
            if (CallHook(CString.FromLiteral("missing")) != 0x777)
                return 104;
        }
        else if (scenario == 2)
        {
            var own = Exec.SetFunction(DOS.DOSLibraryBase, -150,
                APTR.FromPointer(0x2800));
            if (NativeWorkbench31LoadResourceLoadSeg.TryUninstallWhenIdle())
                return 105;
            if (CallHook(CString.FromLiteral("missing")) != 0x777)
                return 106;
            // The newer owner removes its own patch, exposing our hook again.
            if (Exec.SetFunction(DOS.DOSLibraryBase, -150, own).Raw != 0x2800)
                return 107;
        }
        else if (scenario == 6 || scenario == 7)
        {
            if (NativeWorkbench31LoadResourceLoadSeg.TryCache(
                CString.FromLiteral("one"), BPTR.FromRaw(0x111)))
                return 108;
        }
        else if (scenario == 10)
        {
            if (NativeWorkbench31LoadResourceLoadSeg.TryUninstallWhenIdle())
                return 119;
            if (CallHook(CString.FromLiteral("missing")) != 0x777)
                return 120;
        }
        else
        {
            if (!NativeWorkbench31LoadResourceLoadSeg.TryCache(
                CString.FromLiteral("one"), BPTR.FromRaw(0x111)))
                return 109;
            if (NativeWorkbench31LoadResourceLoadSeg.TryUninstallWhenIdle())
                return 110;
            if (CallHook(CString.FromLiteral("different")) != 0x777)
                return 118;
            if (scenario == 8)
            {
                if (CallHook(CString.FromLiteral("unavailable")) != 0x777)
                    return 111;
            }
            if (CallHook(CString.FromLiteral("alias")) != 0x111)
                return 112;
            if (NativeWorkbench31LoadResourceLoadSeg.TryRemove(BPTR.FromRaw(0x111)))
                return 113;
            if (CallHook(CString.FromLiteral("one")) != 0x777)
                return 114;
            if (!NativeWorkbench31LoadResourceLoadSeg.TryCache(
                CString.FromLiteral("two"), BPTR.FromRaw(0x222)))
                return 115;
            if (!NativeWorkbench31LoadResourceLoadSeg.TryRemove(BPTR.FromRaw(0x222)))
                return 116;
        }

        return NativeWorkbench31LoadResourceLoadSeg.TryUninstallWhenIdle() ? 0 : 117;
    }

    private static uint CallHook(CString name) => Invoke(
        APTR.ExportAddress(NativeWorkbench31LoadResourceLoadSeg.HookExport),
        DOS.DOSLibraryBase, name);

    [AmigaIndirectCall(M68kRegister.A3)]
    [return: M68kRegister(M68kRegister.D0)]
    private static extern uint Invoke(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR dosBase,
        [M68kRegister(M68kRegister.D1)] CString name);
}
