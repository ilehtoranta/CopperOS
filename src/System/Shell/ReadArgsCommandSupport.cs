using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Fixed ReadArgs templates used by commands that have no caller-owned
/// template storage.  The template is written into the supplied guest buffer
/// and the result array follows its terminating NUL.  No managed strings or
/// command-local lexer are involved in this boundary.
/// </summary>
internal enum ReadArgsCommandTemplate : byte
{
    Empty,
    Stack,
    Failat,
    Fault,
    Quit,
    Name,
    Cls,
    Unsetenv,
    UnsetenvOptional,
    UnsetOptional,
    Set,
    SetOptional,
    Setenv,
    SetenvOptional,
    Alias,
    Ask,
    Prompt,
    Lab,
    Dir,
    Unalias,
    Skip,
    Path,
    If,
    Run,
    Resident,
    NewShell,
}

internal static class ReadArgsCommandSupport
{
    public static bool Prepare<TPlatform>(
        ref TPlatform platform,
        APTR buffer,
        uint capacity,
        ReadArgsCommandTemplate template,
        uint resultBytes,
        out APTR resultArray,
        out uint templateLength)
        where TPlatform : struct, IShellPlatform
    {
        templateLength = Length(template);
        resultArray = APTR.Null;
        var resultOffset = templateLength + 1;
        if (buffer.IsNull || resultBytes == 0 ||
            resultOffset > uint.MaxValue - resultBytes ||
            capacity < resultOffset + resultBytes ||
            buffer.Raw > uint.MaxValue - capacity ||
            !platform.IsMapped(buffer, capacity))
            return false;

        Write(ref platform, buffer, template);
        resultArray = APTR.FromPointer(buffer.Raw + resultOffset);
        return true;
    }

    private static uint Length(ReadArgsCommandTemplate template)
    {
        switch (template)
        {
            case ReadArgsCommandTemplate.Empty: return 0;
            case ReadArgsCommandTemplate.Stack: return 7;
            case ReadArgsCommandTemplate.Failat: return 9;
            case ReadArgsCommandTemplate.Fault: return 9;
            case ReadArgsCommandTemplate.Quit: return 4;
            case ReadArgsCommandTemplate.Name: return 6;
            case ReadArgsCommandTemplate.Cls: return 7;
            case ReadArgsCommandTemplate.Unsetenv: return 13;
            case ReadArgsCommandTemplate.UnsetenvOptional: return 11;
            case ReadArgsCommandTemplate.UnsetOptional: return 4;
            case ReadArgsCommandTemplate.Set: return 15;
            case ReadArgsCommandTemplate.SetOptional: return 13;
            case ReadArgsCommandTemplate.Setenv: return 22;
            case ReadArgsCommandTemplate.SetenvOptional: return 20;
            case ReadArgsCommandTemplate.Alias: return 13;
            case ReadArgsCommandTemplate.Ask: return 10;
            case ReadArgsCommandTemplate.Prompt: return 8;
            case ReadArgsCommandTemplate.Lab: return 7;
            case ReadArgsCommandTemplate.Dir: return 3;
            case ReadArgsCommandTemplate.Unalias: return 4;
            case ReadArgsCommandTemplate.Skip: return 12;
            case ReadArgsCommandTemplate.Path: return 44;
            case ReadArgsCommandTemplate.If: return 66;
            case ReadArgsCommandTemplate.Run: return 44;
            case ReadArgsCommandTemplate.Resident: return 72;
            case ReadArgsCommandTemplate.NewShell: return 11;
            default: return 0;
        }
    }

    private static void Write<TPlatform>(
        ref TPlatform platform,
        APTR buffer,
        ReadArgsCommandTemplate template)
        where TPlatform : struct, IShellPlatform
    {
        switch (template)
        {
            case ReadArgsCommandTemplate.Empty:
                WriteByte(ref platform, buffer, 0, 0);
                return;
            case ReadArgsCommandTemplate.Stack:
                WriteByte(ref platform, buffer, 0, (byte)'S');
                WriteByte(ref platform, buffer, 1, (byte)'T');
                WriteByte(ref platform, buffer, 2, (byte)'A');
                WriteByte(ref platform, buffer, 3, (byte)'C');
                WriteByte(ref platform, buffer, 4, (byte)'K');
                WriteByte(ref platform, buffer, 5, (byte)'/');
                WriteByte(ref platform, buffer, 6, (byte)'N');
                WriteByte(ref platform, buffer, 7, 0);
                return;
            case ReadArgsCommandTemplate.Failat:
                WriteByte(ref platform, buffer, 0, (byte)'R');
                WriteByte(ref platform, buffer, 1, (byte)'C');
                WriteByte(ref platform, buffer, 2, (byte)'L');
                WriteByte(ref platform, buffer, 3, (byte)'I');
                WriteByte(ref platform, buffer, 4, (byte)'M');
                WriteByte(ref platform, buffer, 5, (byte)'/');
                WriteByte(ref platform, buffer, 6, (byte)'A');
                WriteByte(ref platform, buffer, 7, (byte)'/');
                WriteByte(ref platform, buffer, 8, (byte)'N');
                WriteByte(ref platform, buffer, 9, 0);
                return;
            case ReadArgsCommandTemplate.Fault:
                WriteByte(ref platform, buffer, 0, (byte)'E');
                WriteByte(ref platform, buffer, 1, (byte)'R');
                WriteByte(ref platform, buffer, 2, (byte)'R');
                WriteByte(ref platform, buffer, 3, (byte)'O');
                WriteByte(ref platform, buffer, 4, (byte)'R');
                WriteByte(ref platform, buffer, 5, (byte)'/');
                WriteByte(ref platform, buffer, 6, (byte)'N');
                WriteByte(ref platform, buffer, 7, (byte)'/');
                WriteByte(ref platform, buffer, 8, (byte)'M');
                WriteByte(ref platform, buffer, 9, 0);
                return;
            case ReadArgsCommandTemplate.Quit:
                WriteByte(ref platform, buffer, 0, (byte)'R');
                WriteByte(ref platform, buffer, 1, (byte)'C');
                WriteByte(ref platform, buffer, 2, (byte)'/');
                WriteByte(ref platform, buffer, 3, (byte)'N');
                WriteByte(ref platform, buffer, 4, 0);
                return;
            case ReadArgsCommandTemplate.Name:
                WriteByte(ref platform, buffer, 0, (byte)'N');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'M');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, (byte)'/');
                WriteByte(ref platform, buffer, 5, (byte)'A');
                WriteByte(ref platform, buffer, 6, 0);
                return;
            case ReadArgsCommandTemplate.Cls:
                WriteByte(ref platform, buffer, 0, (byte)'R');
                WriteByte(ref platform, buffer, 1, (byte)'E');
                WriteByte(ref platform, buffer, 2, (byte)'S');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, (byte)'T');
                WriteByte(ref platform, buffer, 5, (byte)'/');
                WriteByte(ref platform, buffer, 6, (byte)'S');
                WriteByte(ref platform, buffer, 7, 0);
                return;
            case ReadArgsCommandTemplate.Unsetenv:
                WriteByte(ref platform, buffer, 0, (byte)'N');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'M');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, (byte)'/');
                WriteByte(ref platform, buffer, 5, (byte)'A');
                WriteByte(ref platform, buffer, 6, (byte)',');
                WriteByte(ref platform, buffer, 7, (byte)'S');
                WriteByte(ref platform, buffer, 8, (byte)'A');
                WriteByte(ref platform, buffer, 9, (byte)'V');
                WriteByte(ref platform, buffer, 10, (byte)'E');
                WriteByte(ref platform, buffer, 11, (byte)'/');
                WriteByte(ref platform, buffer, 12, (byte)'S');
                WriteByte(ref platform, buffer, 13, 0);
                return;
            case ReadArgsCommandTemplate.UnsetenvOptional:
                WriteByte(ref platform, buffer, 0, (byte)'N');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'M');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, (byte)',');
                WriteByte(ref platform, buffer, 5, (byte)'S');
                WriteByte(ref platform, buffer, 6, (byte)'A');
                WriteByte(ref platform, buffer, 7, (byte)'V');
                WriteByte(ref platform, buffer, 8, (byte)'E');
                WriteByte(ref platform, buffer, 9, (byte)'/');
                WriteByte(ref platform, buffer, 10, (byte)'S');
                WriteByte(ref platform, buffer, 11, 0);
                return;
            case ReadArgsCommandTemplate.UnsetOptional:
                WriteByte(ref platform, buffer, 0, (byte)'N');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'M');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, 0);
                return;
            case ReadArgsCommandTemplate.Set:
                WriteByte(ref platform, buffer, 0, (byte)'N');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'M');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, (byte)'/');
                WriteByte(ref platform, buffer, 5, (byte)'A');
                WriteByte(ref platform, buffer, 6, (byte)',');
                WriteByte(ref platform, buffer, 7, (byte)'S');
                WriteByte(ref platform, buffer, 8, (byte)'T');
                WriteByte(ref platform, buffer, 9, (byte)'R');
                WriteByte(ref platform, buffer, 10, (byte)'I');
                WriteByte(ref platform, buffer, 11, (byte)'N');
                WriteByte(ref platform, buffer, 12, (byte)'G');
                WriteByte(ref platform, buffer, 13, (byte)'/');
                WriteByte(ref platform, buffer, 14, (byte)'F');
                WriteByte(ref platform, buffer, 15, 0);
                return;
            case ReadArgsCommandTemplate.SetOptional:
                WriteByte(ref platform, buffer, 0, (byte)'N');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'M');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, (byte)',');
                WriteByte(ref platform, buffer, 5, (byte)'S');
                WriteByte(ref platform, buffer, 6, (byte)'T');
                WriteByte(ref platform, buffer, 7, (byte)'R');
                WriteByte(ref platform, buffer, 8, (byte)'I');
                WriteByte(ref platform, buffer, 9, (byte)'N');
                WriteByte(ref platform, buffer, 10, (byte)'G');
                WriteByte(ref platform, buffer, 11, (byte)'/');
                WriteByte(ref platform, buffer, 12, (byte)'F');
                WriteByte(ref platform, buffer, 13, 0);
                return;
            case ReadArgsCommandTemplate.Setenv:
                WriteByte(ref platform, buffer, 0, (byte)'N');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'M');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, (byte)'/');
                WriteByte(ref platform, buffer, 5, (byte)'A');
                WriteByte(ref platform, buffer, 6, (byte)',');
                WriteByte(ref platform, buffer, 7, (byte)'S');
                WriteByte(ref platform, buffer, 8, (byte)'A');
                WriteByte(ref platform, buffer, 9, (byte)'V');
                WriteByte(ref platform, buffer, 10, (byte)'E');
                WriteByte(ref platform, buffer, 11, (byte)'/');
                WriteByte(ref platform, buffer, 12, (byte)'S');
                WriteByte(ref platform, buffer, 13, (byte)',');
                WriteByte(ref platform, buffer, 14, (byte)'S');
                WriteByte(ref platform, buffer, 15, (byte)'T');
                WriteByte(ref platform, buffer, 16, (byte)'R');
                WriteByte(ref platform, buffer, 17, (byte)'I');
                WriteByte(ref platform, buffer, 18, (byte)'N');
                WriteByte(ref platform, buffer, 19, (byte)'G');
                WriteByte(ref platform, buffer, 20, (byte)'/');
                WriteByte(ref platform, buffer, 21, (byte)'F');
                WriteByte(ref platform, buffer, 22, 0);
                return;
            case ReadArgsCommandTemplate.SetenvOptional:
                WriteByte(ref platform, buffer, 0, (byte)'N');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'M');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, (byte)',');
                WriteByte(ref platform, buffer, 5, (byte)'S');
                WriteByte(ref platform, buffer, 6, (byte)'A');
                WriteByte(ref platform, buffer, 7, (byte)'V');
                WriteByte(ref platform, buffer, 8, (byte)'E');
                WriteByte(ref platform, buffer, 9, (byte)'/');
                WriteByte(ref platform, buffer, 10, (byte)'S');
                WriteByte(ref platform, buffer, 11, (byte)',');
                WriteByte(ref platform, buffer, 12, (byte)'S');
                WriteByte(ref platform, buffer, 13, (byte)'T');
                WriteByte(ref platform, buffer, 14, (byte)'R');
                WriteByte(ref platform, buffer, 15, (byte)'I');
                WriteByte(ref platform, buffer, 16, (byte)'N');
                WriteByte(ref platform, buffer, 17, (byte)'G');
                WriteByte(ref platform, buffer, 18, (byte)'/');
                WriteByte(ref platform, buffer, 19, (byte)'F');
                WriteByte(ref platform, buffer, 20, 0);
                return;
            case ReadArgsCommandTemplate.Alias:
                WriteByte(ref platform, buffer, 0, (byte)'N');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'M');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, (byte)',');
                WriteByte(ref platform, buffer, 5, (byte)'S');
                WriteByte(ref platform, buffer, 6, (byte)'T');
                WriteByte(ref platform, buffer, 7, (byte)'R');
                WriteByte(ref platform, buffer, 8, (byte)'I');
                WriteByte(ref platform, buffer, 9, (byte)'N');
                WriteByte(ref platform, buffer, 10, (byte)'G');
                WriteByte(ref platform, buffer, 11, (byte)'/');
                WriteByte(ref platform, buffer, 12, (byte)'F');
                WriteByte(ref platform, buffer, 13, 0);
                return;
            case ReadArgsCommandTemplate.Ask:
                WriteByte(ref platform, buffer, 0, (byte)'P');
                WriteByte(ref platform, buffer, 1, (byte)'R');
                WriteByte(ref platform, buffer, 2, (byte)'O');
                WriteByte(ref platform, buffer, 3, (byte)'M');
                WriteByte(ref platform, buffer, 4, (byte)'P');
                WriteByte(ref platform, buffer, 5, (byte)'T');
                WriteByte(ref platform, buffer, 6, (byte)'/');
                WriteByte(ref platform, buffer, 7, (byte)'A');
                WriteByte(ref platform, buffer, 8, (byte)'/');
                WriteByte(ref platform, buffer, 9, (byte)'F');
                WriteByte(ref platform, buffer, 10, 0);
                return;
            case ReadArgsCommandTemplate.Prompt:
                WriteByte(ref platform, buffer, 0, (byte)'P');
                WriteByte(ref platform, buffer, 1, (byte)'R');
                WriteByte(ref platform, buffer, 2, (byte)'O');
                WriteByte(ref platform, buffer, 3, (byte)'M');
                WriteByte(ref platform, buffer, 4, (byte)'P');
                WriteByte(ref platform, buffer, 5, (byte)'T');
                WriteByte(ref platform, buffer, 6, (byte)'/');
                WriteByte(ref platform, buffer, 7, (byte)'F');
                WriteByte(ref platform, buffer, 8, 0);
                return;
            case ReadArgsCommandTemplate.Lab:
                WriteByte(ref platform, buffer, 0, (byte)'L');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'B');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, (byte)'L');
                WriteByte(ref platform, buffer, 5, (byte)'/');
                WriteByte(ref platform, buffer, 6, (byte)'A');
                WriteByte(ref platform, buffer, 7, 0);
                return;
            case ReadArgsCommandTemplate.Dir:
                WriteByte(ref platform, buffer, 0, (byte)'D');
                WriteByte(ref platform, buffer, 1, (byte)'I');
                WriteByte(ref platform, buffer, 2, (byte)'R');
                WriteByte(ref platform, buffer, 3, 0);
                return;
            case ReadArgsCommandTemplate.Unalias:
                WriteByte(ref platform, buffer, 0, (byte)'N');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'M');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, 0);
                return;
            case ReadArgsCommandTemplate.Skip:
                WriteByte(ref platform, buffer, 0, (byte)'L');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'B');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, (byte)'L');
                WriteByte(ref platform, buffer, 5, (byte)',');
                WriteByte(ref platform, buffer, 6, (byte)'B');
                WriteByte(ref platform, buffer, 7, (byte)'A');
                WriteByte(ref platform, buffer, 8, (byte)'C');
                WriteByte(ref platform, buffer, 9, (byte)'K');
                WriteByte(ref platform, buffer, 10, (byte)'/');
                WriteByte(ref platform, buffer, 11, (byte)'S');
                WriteByte(ref platform, buffer, 12, 0);
                return;
            case ReadArgsCommandTemplate.Path:
                WriteByte(ref platform, buffer, 0, (byte)'P');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'T');
                WriteByte(ref platform, buffer, 3, (byte)'H');
                WriteByte(ref platform, buffer, 4, (byte)'/');
                WriteByte(ref platform, buffer, 5, (byte)'M');
                WriteByte(ref platform, buffer, 6, (byte)',');
                WriteByte(ref platform, buffer, 7, (byte)'A');
                WriteByte(ref platform, buffer, 8, (byte)'D');
                WriteByte(ref platform, buffer, 9, (byte)'D');
                WriteByte(ref platform, buffer, 10, (byte)'/');
                WriteByte(ref platform, buffer, 11, (byte)'S');
                WriteByte(ref platform, buffer, 12, (byte)',');
                WriteByte(ref platform, buffer, 13, (byte)'S');
                WriteByte(ref platform, buffer, 14, (byte)'H');
                WriteByte(ref platform, buffer, 15, (byte)'O');
                WriteByte(ref platform, buffer, 16, (byte)'W');
                WriteByte(ref platform, buffer, 17, (byte)'/');
                WriteByte(ref platform, buffer, 18, (byte)'S');
                WriteByte(ref platform, buffer, 19, (byte)',');
                WriteByte(ref platform, buffer, 20, (byte)'R');
                WriteByte(ref platform, buffer, 21, (byte)'E');
                WriteByte(ref platform, buffer, 22, (byte)'S');
                WriteByte(ref platform, buffer, 23, (byte)'E');
                WriteByte(ref platform, buffer, 24, (byte)'T');
                WriteByte(ref platform, buffer, 25, (byte)'/');
                WriteByte(ref platform, buffer, 26, (byte)'S');
                WriteByte(ref platform, buffer, 27, (byte)',');
                WriteByte(ref platform, buffer, 28, (byte)'R');
                WriteByte(ref platform, buffer, 29, (byte)'E');
                WriteByte(ref platform, buffer, 30, (byte)'M');
                WriteByte(ref platform, buffer, 31, (byte)'O');
                WriteByte(ref platform, buffer, 32, (byte)'V');
                WriteByte(ref platform, buffer, 33, (byte)'E');
                WriteByte(ref platform, buffer, 34, (byte)'/');
                WriteByte(ref platform, buffer, 35, (byte)'S');
                WriteByte(ref platform, buffer, 36, (byte)',');
                WriteByte(ref platform, buffer, 37, (byte)'Q');
                WriteByte(ref platform, buffer, 38, (byte)'U');
                WriteByte(ref platform, buffer, 39, (byte)'I');
                WriteByte(ref platform, buffer, 40, (byte)'E');
                WriteByte(ref platform, buffer, 41, (byte)'T');
                WriteByte(ref platform, buffer, 42, (byte)'/');
                WriteByte(ref platform, buffer, 43, (byte)'S');
                WriteByte(ref platform, buffer, 44, 0);
                return;
            case ReadArgsCommandTemplate.If:
                WriteByte(ref platform, buffer, 0, (byte)'N');
                WriteByte(ref platform, buffer, 1, (byte)'O');
                WriteByte(ref platform, buffer, 2, (byte)'T');
                WriteByte(ref platform, buffer, 3, (byte)'/');
                WriteByte(ref platform, buffer, 4, (byte)'S');
                WriteByte(ref platform, buffer, 5, (byte)',');
                WriteByte(ref platform, buffer, 6, (byte)'W');
                WriteByte(ref platform, buffer, 7, (byte)'A');
                WriteByte(ref platform, buffer, 8, (byte)'R');
                WriteByte(ref platform, buffer, 9, (byte)'N');
                WriteByte(ref platform, buffer, 10, (byte)'/');
                WriteByte(ref platform, buffer, 11, (byte)'S');
                WriteByte(ref platform, buffer, 12, (byte)',');
                WriteByte(ref platform, buffer, 13, (byte)'E');
                WriteByte(ref platform, buffer, 14, (byte)'R');
                WriteByte(ref platform, buffer, 15, (byte)'R');
                WriteByte(ref platform, buffer, 16, (byte)'O');
                WriteByte(ref platform, buffer, 17, (byte)'R');
                WriteByte(ref platform, buffer, 18, (byte)'/');
                WriteByte(ref platform, buffer, 19, (byte)'S');
                WriteByte(ref platform, buffer, 20, (byte)',');
                WriteByte(ref platform, buffer, 21, (byte)'F');
                WriteByte(ref platform, buffer, 22, (byte)'A');
                WriteByte(ref platform, buffer, 23, (byte)'I');
                WriteByte(ref platform, buffer, 24, (byte)'L');
                WriteByte(ref platform, buffer, 25, (byte)'/');
                WriteByte(ref platform, buffer, 26, (byte)'S');
                WriteByte(ref platform, buffer, 27, (byte)',');
                WriteByte(ref platform, buffer, 28, (byte)',');
                WriteByte(ref platform, buffer, 29, (byte)'E');
                WriteByte(ref platform, buffer, 30, (byte)'Q');
                WriteByte(ref platform, buffer, 31, (byte)'/');
                WriteByte(ref platform, buffer, 32, (byte)'K');
                WriteByte(ref platform, buffer, 33, (byte)',');
                WriteByte(ref platform, buffer, 34, (byte)'G');
                WriteByte(ref platform, buffer, 35, (byte)'T');
                WriteByte(ref platform, buffer, 36, (byte)'/');
                WriteByte(ref platform, buffer, 37, (byte)'K');
                WriteByte(ref platform, buffer, 38, (byte)',');
                WriteByte(ref platform, buffer, 39, (byte)'G');
                WriteByte(ref platform, buffer, 40, (byte)'E');
                WriteByte(ref platform, buffer, 41, (byte)'/');
                WriteByte(ref platform, buffer, 42, (byte)'K');
                WriteByte(ref platform, buffer, 43, (byte)',');
                WriteByte(ref platform, buffer, 44, (byte)'V');
                WriteByte(ref platform, buffer, 45, (byte)'A');
                WriteByte(ref platform, buffer, 46, (byte)'L');
                WriteByte(ref platform, buffer, 47, (byte)'/');
                WriteByte(ref platform, buffer, 48, (byte)'S');
                WriteByte(ref platform, buffer, 49, (byte)',');
                WriteByte(ref platform, buffer, 50, (byte)'E');
                WriteByte(ref platform, buffer, 51, (byte)'X');
                WriteByte(ref platform, buffer, 52, (byte)'I');
                WriteByte(ref platform, buffer, 53, (byte)'S');
                WriteByte(ref platform, buffer, 54, (byte)'T');
                WriteByte(ref platform, buffer, 55, (byte)'S');
                WriteByte(ref platform, buffer, 56, (byte)'/');
                WriteByte(ref platform, buffer, 57, (byte)'K');
                WriteByte(ref platform, buffer, 58, (byte)',');
                WriteByte(ref platform, buffer, 59, (byte)'N');
                WriteByte(ref platform, buffer, 60, (byte)'O');
                WriteByte(ref platform, buffer, 61, (byte)'R');
                WriteByte(ref platform, buffer, 62, (byte)'E');
                WriteByte(ref platform, buffer, 63, (byte)'Q');
                WriteByte(ref platform, buffer, 64, (byte)'/');
                WriteByte(ref platform, buffer, 65, (byte)'S');
                WriteByte(ref platform, buffer, 66, 0);
                return;
            case ReadArgsCommandTemplate.Run:
                WriteByte(ref platform, buffer, 0, (byte)'D');
                WriteByte(ref platform, buffer, 1, (byte)'E');
                WriteByte(ref platform, buffer, 2, (byte)'T');
                WriteByte(ref platform, buffer, 3, (byte)'A');
                WriteByte(ref platform, buffer, 4, (byte)'C');
                WriteByte(ref platform, buffer, 5, (byte)'H');
                WriteByte(ref platform, buffer, 6, (byte)'/');
                WriteByte(ref platform, buffer, 7, (byte)'S');
                WriteByte(ref platform, buffer, 8, (byte)',');
                WriteByte(ref platform, buffer, 9, (byte)'Q');
                WriteByte(ref platform, buffer, 10, (byte)'U');
                WriteByte(ref platform, buffer, 11, (byte)'I');
                WriteByte(ref platform, buffer, 12, (byte)'E');
                WriteByte(ref platform, buffer, 13, (byte)'T');
                WriteByte(ref platform, buffer, 14, (byte)'/');
                WriteByte(ref platform, buffer, 15, (byte)'S');
                WriteByte(ref platform, buffer, 16, (byte)',');
                WriteByte(ref platform, buffer, 17, (byte)'S');
                WriteByte(ref platform, buffer, 18, (byte)'T');
                WriteByte(ref platform, buffer, 19, (byte)'A');
                WriteByte(ref platform, buffer, 20, (byte)'C');
                WriteByte(ref platform, buffer, 21, (byte)'K');
                WriteByte(ref platform, buffer, 22, (byte)'/');
                WriteByte(ref platform, buffer, 23, (byte)'K');
                WriteByte(ref platform, buffer, 24, (byte)'/');
                WriteByte(ref platform, buffer, 25, (byte)'N');
                WriteByte(ref platform, buffer, 26, (byte)',');
                WriteByte(ref platform, buffer, 27, (byte)'P');
                WriteByte(ref platform, buffer, 28, (byte)'R');
                WriteByte(ref platform, buffer, 29, (byte)'I');
                WriteByte(ref platform, buffer, 30, (byte)'/');
                WriteByte(ref platform, buffer, 31, (byte)'K');
                WriteByte(ref platform, buffer, 32, (byte)'/');
                WriteByte(ref platform, buffer, 33, (byte)'N');
                WriteByte(ref platform, buffer, 34, (byte)',');
                WriteByte(ref platform, buffer, 35, (byte)'C');
                WriteByte(ref platform, buffer, 36, (byte)'O');
                WriteByte(ref platform, buffer, 37, (byte)'M');
                WriteByte(ref platform, buffer, 38, (byte)'M');
                WriteByte(ref platform, buffer, 39, (byte)'A');
                WriteByte(ref platform, buffer, 40, (byte)'N');
                WriteByte(ref platform, buffer, 41, (byte)'D');
                WriteByte(ref platform, buffer, 42, (byte)'/');
                WriteByte(ref platform, buffer, 43, (byte)'F');
                WriteByte(ref platform, buffer, 44, 0);
                return;
            case ReadArgsCommandTemplate.Resident:
                WriteByte(ref platform, buffer, 0, (byte)'N');
                WriteByte(ref platform, buffer, 1, (byte)'A');
                WriteByte(ref platform, buffer, 2, (byte)'M');
                WriteByte(ref platform, buffer, 3, (byte)'E');
                WriteByte(ref platform, buffer, 4, (byte)',');
                WriteByte(ref platform, buffer, 5, (byte)'F');
                WriteByte(ref platform, buffer, 6, (byte)'I');
                WriteByte(ref platform, buffer, 7, (byte)'L');
                WriteByte(ref platform, buffer, 8, (byte)'E');
                WriteByte(ref platform, buffer, 9, (byte)',');
                WriteByte(ref platform, buffer, 10, (byte)'A');
                WriteByte(ref platform, buffer, 11, (byte)'L');
                WriteByte(ref platform, buffer, 12, (byte)'I');
                WriteByte(ref platform, buffer, 13, (byte)'A');
                WriteByte(ref platform, buffer, 14, (byte)'S');
                WriteByte(ref platform, buffer, 15, (byte)'/');
                WriteByte(ref platform, buffer, 16, (byte)'K');
                WriteByte(ref platform, buffer, 17, (byte)',');
                WriteByte(ref platform, buffer, 18, (byte)'R');
                WriteByte(ref platform, buffer, 19, (byte)'E');
                WriteByte(ref platform, buffer, 20, (byte)'M');
                WriteByte(ref platform, buffer, 21, (byte)'O');
                WriteByte(ref platform, buffer, 22, (byte)'V');
                WriteByte(ref platform, buffer, 23, (byte)'E');
                WriteByte(ref platform, buffer, 24, (byte)'/');
                WriteByte(ref platform, buffer, 25, (byte)'S');
                WriteByte(ref platform, buffer, 26, (byte)',');
                WriteByte(ref platform, buffer, 27, (byte)'A');
                WriteByte(ref platform, buffer, 28, (byte)'D');
                WriteByte(ref platform, buffer, 29, (byte)'D');
                WriteByte(ref platform, buffer, 30, (byte)'/');
                WriteByte(ref platform, buffer, 31, (byte)'S');
                WriteByte(ref platform, buffer, 32, (byte)',');
                WriteByte(ref platform, buffer, 33, (byte)'R');
                WriteByte(ref platform, buffer, 34, (byte)'E');
                WriteByte(ref platform, buffer, 35, (byte)'P');
                WriteByte(ref platform, buffer, 36, (byte)'L');
                WriteByte(ref platform, buffer, 37, (byte)'A');
                WriteByte(ref platform, buffer, 38, (byte)'C');
                WriteByte(ref platform, buffer, 39, (byte)'E');
                WriteByte(ref platform, buffer, 40, (byte)'/');
                WriteByte(ref platform, buffer, 41, (byte)'S');
                WriteByte(ref platform, buffer, 42, (byte)',');
                WriteByte(ref platform, buffer, 43, (byte)'P');
                WriteByte(ref platform, buffer, 44, (byte)'U');
                WriteByte(ref platform, buffer, 45, (byte)'R');
                WriteByte(ref platform, buffer, 46, (byte)'E');
                WriteByte(ref platform, buffer, 47, (byte)'=');
                WriteByte(ref platform, buffer, 48, (byte)'F');
                WriteByte(ref platform, buffer, 49, (byte)'O');
                WriteByte(ref platform, buffer, 50, (byte)'R');
                WriteByte(ref platform, buffer, 51, (byte)'C');
                WriteByte(ref platform, buffer, 52, (byte)'E');
                WriteByte(ref platform, buffer, 53, (byte)'/');
                WriteByte(ref platform, buffer, 54, (byte)'S');
                WriteByte(ref platform, buffer, 55, (byte)',');
                WriteByte(ref platform, buffer, 56, (byte)'S');
                WriteByte(ref platform, buffer, 57, (byte)'Y');
                WriteByte(ref platform, buffer, 58, (byte)'S');
                WriteByte(ref platform, buffer, 59, (byte)'T');
                WriteByte(ref platform, buffer, 60, (byte)'E');
                WriteByte(ref platform, buffer, 61, (byte)'M');
                WriteByte(ref platform, buffer, 62, (byte)'/');
                WriteByte(ref platform, buffer, 63, (byte)'S');
                WriteByte(ref platform, buffer, 64, (byte)',');
                WriteByte(ref platform, buffer, 65, (byte)'D');
                WriteByte(ref platform, buffer, 66, (byte)'E');
                WriteByte(ref platform, buffer, 67, (byte)'F');
                WriteByte(ref platform, buffer, 68, (byte)'E');
                WriteByte(ref platform, buffer, 69, (byte)'R');
                WriteByte(ref platform, buffer, 70, (byte)'/');
                WriteByte(ref platform, buffer, 71, (byte)'S');
                WriteByte(ref platform, buffer, 72, 0);
                return;
            case ReadArgsCommandTemplate.NewShell:
                WriteByte(ref platform, buffer, 0, (byte)'W');
                WriteByte(ref platform, buffer, 1, (byte)'I');
                WriteByte(ref platform, buffer, 2, (byte)'N');
                WriteByte(ref platform, buffer, 3, (byte)'D');
                WriteByte(ref platform, buffer, 4, (byte)'O');
                WriteByte(ref platform, buffer, 5, (byte)'W');
                WriteByte(ref platform, buffer, 6, (byte)',');
                WriteByte(ref platform, buffer, 7, (byte)'F');
                WriteByte(ref platform, buffer, 8, (byte)'R');
                WriteByte(ref platform, buffer, 9, (byte)'O');
                WriteByte(ref platform, buffer, 10, (byte)'M');
                WriteByte(ref platform, buffer, 11, 0);
                return;
        }
    }

    private static void WriteByte<TPlatform>(
        ref TPlatform platform,
        APTR buffer,
        int offset,
        byte value)
        where TPlatform : struct, IShellPlatform =>
        platform.WriteUInt8(buffer, offset, value);

    public static int CStringLength<TPlatform>(
        ref TPlatform platform,
        APTR value,
        uint maximum)
        where TPlatform : struct, IShellPlatform
    {
        if (value.IsNull) return -1;
        for (var index = 0u; index < maximum; index++)
        {
            if (value.Raw > uint.MaxValue - index ||
                !platform.IsMapped(value, index + 1))
                return -1;
            if (platform.ReadUInt8(value, (int)index) == 0)
                return (int)index;
        }
        return -1;
    }

    public static bool CopyCString<TPlatform>(
        ref TPlatform platform,
        APTR source,
        APTR destination,
        uint destinationCapacity,
        out uint length)
        where TPlatform : struct, IShellPlatform
    {
        length = 0;
        var measured = CStringLength(ref platform, source, 65536);
        if (measured < 0 || (uint)measured > destinationCapacity ||
            (measured != 0 && (destination.IsNull ||
             !platform.IsMapped(destination, (uint)measured))))
            return false;
        for (var index = 0; index < measured; index++)
            platform.WriteUInt8(destination, index,
                platform.ReadUInt8(source, index));
        if (destination.IsNotNull && destinationCapacity > (uint)measured)
            platform.WriteUInt8(destination, measured, 0);
        length = (uint)measured;
        return true;
    }
}
