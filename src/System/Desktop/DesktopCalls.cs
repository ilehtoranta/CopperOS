using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Desktop;

[AmigaLibrary("dos.library", AmigaLibraryBasePolicy.CallerProvided)]
internal static class DesktopDos
{
    [AmigaLvo(DosLvo.CreateDir)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint CreateDirectory([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint name);
    [AmigaLvo(DosLvo.DeleteFile)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Delete([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint name);
    [AmigaLvo(DosLvo.Rename)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Rename([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint from, [M68kRegister(M68kRegister.D2)] uint to);
    [AmigaLvo(DosLvo.SameLock)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Same([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint first, [M68kRegister(M68kRegister.D2)] uint second);
    [AmigaLvo(DosLvo.SetComment)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Comment([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint name, [M68kRegister(M68kRegister.D2)] uint text);
    [AmigaLvo(DosLvo.SetProtection)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Protection([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint name, [M68kRegister(M68kRegister.D2)] uint bits);
    [AmigaLvo(DosLvo.Open)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Open([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint name, [M68kRegister(M68kRegister.D2)] int mode);
    [AmigaLvo(DosLvo.Read)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Read([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint file,
        [M68kRegister(M68kRegister.D2)] uint data, [M68kRegister(M68kRegister.D3)] int bytes);
    [AmigaLvo(DosLvo.Close)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Close([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint file);
    [AmigaLvo(DosLvo.DupLock)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Duplicate([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint handle);
    [AmigaLvo(DosLvo.Lock)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Lock([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint name, [M68kRegister(M68kRegister.D2)] int mode);
    [AmigaLvo(DosLvo.UnLock)] internal static extern void Unlock([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint handle);
    [AmigaLvo(DosLvo.CurrentDir)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Current([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint handle);
    [AmigaLvo(DosLvo.Examine)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Examine([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint handle, [M68kRegister(M68kRegister.D2)] uint fib);
    [AmigaLvo(DosLvo.ExNext)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Next([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint handle, [M68kRegister(M68kRegister.D2)] uint fib);
    [AmigaLvo(DosLvo.NameFromLock)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Name([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint handle, [M68kRegister(M68kRegister.D2)] uint text, [M68kRegister(M68kRegister.D3)] int length);
    [AmigaLvo(DosLvo.ParentDir)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Parent([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint handle);
    [AmigaLvo(DosLvo.IoErr)] [return: M68kRegister(M68kRegister.D0)] internal static extern uint Error([M68kRegister(M68kRegister.A6)] uint lib);
    [AmigaLvo(DosLvo.LoadSeg)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Load([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint name);
    [AmigaLvo(DosLvo.UnLoadSeg)] internal static extern void Unload([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint segment);
    [AmigaLvo(DosLvo.FindSegment)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Find([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint name,
        [M68kRegister(M68kRegister.D2)] uint start, [M68kRegister(M68kRegister.D3)] int system);
    [AmigaLvo(DosLvo.AddSegment)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Add([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint name,
        [M68kRegister(M68kRegister.D2)] uint segment, [M68kRegister(M68kRegister.D3)] int type);
    [AmigaLvo(DosLvo.CreateNewProc)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Create([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint tags);
    [AmigaLvo(DosLvo.Output)] [return: M68kRegister(M68kRegister.D0)] internal static extern uint Output([M68kRegister(M68kRegister.A6)] uint lib);
    [AmigaLvo(DosLvo.Input)] [return: M68kRegister(M68kRegister.D0)] internal static extern uint Input([M68kRegister(M68kRegister.A6)] uint lib);
    [AmigaLvo(DosLvo.Write)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Write([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.D1)] uint file,
        [M68kRegister(M68kRegister.D2)] uint text, [M68kRegister(M68kRegister.D3)] int length);
}

[AmigaLibrary("intuition.library", AmigaLibraryBasePolicy.CallerProvided)]
internal static class DesktopIntuition
{
    [AmigaLvo(IntuitionLvo.OpenScreen)] [return: M68kRegister(M68kRegister.D0)] internal static extern uint Screen([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.A0)] uint spec);
    [AmigaLvo(IntuitionLvo.OpenWindow)] [return: M68kRegister(M68kRegister.D0)] internal static extern uint Window([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.A0)] uint spec);
    [AmigaLvo(IntuitionLvo.CloseWindow)] internal static extern void CloseWindow([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.A0)] uint window);
    [AmigaLvo(IntuitionLvo.CloseScreen)] [return: M68kRegister(M68kRegister.D0)] internal static extern int CloseScreen([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.A0)] uint screen);
    [AmigaLvo(IntuitionLvo.ModifyIDCMP)] [return: M68kRegister(M68kRegister.D0)] internal static extern int Modify([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.A0)] uint window, [M68kRegister(M68kRegister.D0)] uint flags);
    [AmigaLvo(IntuitionLvo.BeginRefresh)] internal static extern void Begin([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.A0)] uint window);
    [AmigaLvo(IntuitionLvo.EndRefresh)] internal static extern void End([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.A0)] uint window, [M68kRegister(M68kRegister.D0)] int complete);
}

[AmigaLibrary("graphics.library", AmigaLibraryBasePolicy.CallerProvided)]
internal static class DesktopGraphics
{
    [AmigaLvo(-270)] internal static extern void WaitFrame([M68kRegister(M68kRegister.A6)] uint graphics);
    [AmigaLvo(-66)] internal static extern void Font([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.A1)] uint rp, [M68kRegister(M68kRegister.A0)] uint font);
    [AmigaLvo(-342)] internal static extern void Pen([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.A1)] uint rp, [M68kRegister(M68kRegister.D0)] uint pen);
    [AmigaLvo(-354)] internal static extern void Mode([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.A1)] uint rp, [M68kRegister(M68kRegister.D0)] uint mode);
    [AmigaLvo(-306)] internal static extern void Rect([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.A1)] uint rp,
        [M68kRegister(M68kRegister.D0)] int x0, [M68kRegister(M68kRegister.D1)] int y0, [M68kRegister(M68kRegister.D2)] int x1, [M68kRegister(M68kRegister.D3)] int y1);
    [AmigaLvo(-240)] internal static extern void Move([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.A1)] uint rp, [M68kRegister(M68kRegister.D0)] int x, [M68kRegister(M68kRegister.D1)] int y);
    [AmigaLvo(-60)] internal static extern void Text([M68kRegister(M68kRegister.A6)] uint lib, [M68kRegister(M68kRegister.A0)] uint text, [M68kRegister(M68kRegister.A1)] uint rp, [M68kRegister(M68kRegister.D0)] int length);
}

[AmigaLibrary("exec.library::desktop", AmigaLibraryBasePolicy.CallerProvided)]
internal static class DesktopNative
{
    [AmigaIndirectCall(M68kRegister.A2)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Provider([M68kRegister(M68kRegister.A2)] uint wrapper, [M68kRegister(M68kRegister.A3)] uint code, [M68kRegister(M68kRegister.A6)] uint exec);
}
