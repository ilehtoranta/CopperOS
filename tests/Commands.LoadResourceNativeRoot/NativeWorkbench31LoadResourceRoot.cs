using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.LoadResourceNativeRoot;

/// <summary>
/// Qualification-only root that keeps the LoadResource hook lifecycle
/// reachable for three-CPU resident HUNK compilation. It is not a command.
/// </summary>
public static class NativeWorkbench31LoadResourceRoot
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        if (argumentLength == int.MinValue)
            return NativeWorkbench31LoadResourceLoadSeg.TryInstall() ? 0 : 20;
        if (argumentLength == int.MinValue + 1)
            return NativeWorkbench31LoadResourceLoadSeg.TryCache(
                CString.FromPointer(0), BPTR.Null) ? 0 : 20;
        if (argumentLength == int.MinValue + 2)
            return NativeWorkbench31LoadResourceLoadSeg.TryRemove(BPTR.Null) ? 0 : 20;
        if (argumentLength == int.MinValue + 3)
            return NativeWorkbench31LoadResourceLoadSeg.TryUninstallWhenIdle() ? 0 : 20;
        if (argumentLength == int.MinValue + 4)
        {
            var arguments = default(NativeCommandArguments);
            var result = NativeWorkbench31LoadResourceProtocol.Send(APTR.Null,
                ref arguments, out _);
            return result;
        }
        return argumentText.IsNull ? 0 : 0;
    }
}
