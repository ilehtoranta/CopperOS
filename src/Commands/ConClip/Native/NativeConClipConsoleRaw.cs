using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// The private console.device vectors used by the released ConClip source.
///
/// These calls are deliberately isolated from the command entry.  The
/// persistent worker still needs its guest Hook/SGWork and clipboard lifetime
/// implementation before this surface can be used by a shipping command.
/// </summary>
internal static class NativeConClipConsoleRaw
{
    public const short GetConSnipLvo = -54;
    public const short SetConSnipLvo = -60;
    public const short AddConSnipHookLvo = -66;
    public const short RemConSnipHookLvo = -72;

    public static APTR GetConSnip(APTR consoleDevice) =>
        GetConSnipCall(Entry(consoleDevice, GetConSnipLvo), consoleDevice);

    public static int SetConSnip(APTR consoleDevice, APTR data) =>
        SetConSnipCall(Entry(consoleDevice, SetConSnipLvo), consoleDevice, data);

    public static void AddConSnipHook(APTR consoleDevice, APTR hook) =>
        AddConSnipHookCall(Entry(consoleDevice, AddConSnipHookLvo),
            consoleDevice, hook);

    public static void RemConSnipHook(APTR consoleDevice, APTR hook) =>
        RemConSnipHookCall(Entry(consoleDevice, RemConSnipHookLvo),
            consoleDevice, hook);

    private static APTR Entry(APTR library, short lvo) =>
        APTR.FromPointer(unchecked(library.Raw - (uint)-lvo));

    [AmigaIndirectCall(M68kRegister.A3)]
    [return: M68kRegister(M68kRegister.D0)]
    private static extern APTR GetConSnipCall(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR consoleDevice);

    [AmigaIndirectCall(M68kRegister.A3)]
    [return: M68kRegister(M68kRegister.D0)]
    private static extern int SetConSnipCall(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR consoleDevice,
        [M68kRegister(M68kRegister.A0)] APTR data);

    [AmigaIndirectCall(M68kRegister.A3)]
    private static extern void AddConSnipHookCall(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR consoleDevice,
        [M68kRegister(M68kRegister.A0)] APTR hook);

    [AmigaIndirectCall(M68kRegister.A3)]
    private static extern void RemConSnipHookCall(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR consoleDevice,
        [M68kRegister(M68kRegister.A0)] APTR hook);
}
