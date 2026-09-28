using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Fixed ReadArgs templates used by commands that have no caller-owned
/// template storage.  The template is written into the supplied guest buffer
/// and the result array follows its terminating NUL at a four-byte-aligned
/// guest address. No managed strings or command-local lexer are involved.
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
    Echo,
}

// ReadArgs stores one ULONG result per template item.  Keep sequential ABI
// traversal in this bounded cursor so command code works with named records
// rather than positional byte offsets.
[System.Runtime.InteropServices.StructLayout(
    System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct ShellReadArgsResultCursor
{
    internal APTR Base;
    internal uint ByteLength;
    internal uint Position;
}

internal static class ShellReadArgsResultCursorCodec
{
    internal static bool TryCreate<TPlatform>(ref TPlatform platform,
        APTR address, uint byteLength, out ShellReadArgsResultCursor cursor)
        where TPlatform : struct, IShellPlatform
    {
        cursor = default;
        if (address.IsNull || byteLength == 0 ||
            address.Raw > uint.MaxValue - byteLength ||
            !platform.IsMapped(address, byteLength)) return false;
        cursor.Base = address;
        cursor.ByteLength = byteLength;
        return true;
    }

    internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
        ref ShellReadArgsResultCursor cursor, out uint value)
        where TPlatform : struct, IShellPlatform
    {
        value = 0;
        if (cursor.Position > cursor.ByteLength ||
            cursor.ByteLength - cursor.Position < sizeof(uint) ||
            cursor.Position > int.MaxValue) return false;
        value = platform.ReadUInt32(cursor.Base, (int)cursor.Position);
        cursor.Position += sizeof(uint);
        return true;
    }

    internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
        ref ShellReadArgsResultCursor cursor, out APTR value)
        where TPlatform : struct, IShellPlatform
    {
        value = APTR.Null;
        if (!TryReadUInt32(ref platform, ref cursor, out var raw))
            return false;
        value = APTR.FromPointer(raw);
        return true;
    }

    internal static bool IsComplete(ShellReadArgsResultCursor cursor) =>
        cursor.Position == cursor.ByteLength;
}

// These templates are NUL-terminated guest byte strings. Keep their only
// positional state in this named sequential writer record.
[System.Runtime.InteropServices.StructLayout(
    System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct ShellReadArgsTemplateWriter
{
    internal APTR Buffer;
    internal uint Position;
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
        var templateBytes = templateLength + 1;
        if (buffer.IsNull || resultBytes == 0 ||
            capacity < templateBytes ||
            buffer.Raw > uint.MaxValue - capacity ||
            !platform.IsMapped(buffer, capacity))
            return false;

        // Strings may start at any byte address, but DOS stores LONG result
        // slots. Align the absolute address, not just the relative offset;
        // validate the padding and full result span before writing anything.
        var resultAddress = buffer.Raw + templateBytes;
        var padding = (4u - (resultAddress & 3u)) & 3u;
        var remaining = capacity - templateBytes;
        if (padding > remaining || resultBytes > remaining - padding)
            return false;

        Write(ref platform, buffer, template);
        resultArray = APTR.FromPointer(resultAddress + padding);
        return true;
    }

    public static bool TryWriteTemplate<TPlatform>(
        ref TPlatform platform,
        APTR buffer,
        uint capacity,
        ReadArgsCommandTemplate template,
        out uint templateLength)
        where TPlatform : struct, IShellPlatform
    {
        templateLength = Length(template);
        var templateBytes = templateLength + 1;
        if (buffer.IsNull || capacity < templateBytes ||
            buffer.Raw > uint.MaxValue - capacity ||
            !platform.IsMapped(buffer, capacity))
            return false;
        Write(ref platform, buffer, template);
        return true;
    }

    private static uint Length(ReadArgsCommandTemplate template)
    {
        switch (template)
        {
            case ReadArgsCommandTemplate.Empty: return 0;
            case ReadArgsCommandTemplate.Stack: return 6;
            case ReadArgsCommandTemplate.Failat: return 7;
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
            case ReadArgsCommandTemplate.Echo: return 41;
            default: return 0;
        }
    }

    private static void Write<TPlatform>(
        ref TPlatform platform,
        APTR buffer,
        ReadArgsCommandTemplate template)
        where TPlatform : struct, IShellPlatform
    {
        var writer = new ShellReadArgsTemplateWriter { Buffer = buffer };
        switch (template)
        {
            case ReadArgsCommandTemplate.Empty:
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Stack:
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'Z');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Failat:
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'C');
                AppendByte(ref platform, ref writer, (byte)'L');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Fault:
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Quit:
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'C');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Name:
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Cls:
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Unsetenv:
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'V');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.UnsetenvOptional:
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'V');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.UnsetOptional:
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Set:
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'G');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.SetOptional:
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'G');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Setenv:
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'V');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'G');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.SetenvOptional:
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'V');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'G');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Alias:
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'G');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Ask:
                AppendByte(ref platform, ref writer, (byte)'P');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'P');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Prompt:
                AppendByte(ref platform, ref writer, (byte)'P');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'P');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Lab:
                AppendByte(ref platform, ref writer, (byte)'L');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'B');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'L');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Dir:
                AppendByte(ref platform, ref writer, (byte)'D');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Unalias:
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Skip:
                AppendByte(ref platform, ref writer, (byte)'L');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'B');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'L');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'B');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'C');
                AppendByte(ref platform, ref writer, (byte)'K');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Path:
                AppendByte(ref platform, ref writer, (byte)'P');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'H');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'D');
                AppendByte(ref platform, ref writer, (byte)'D');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'H');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'W');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'V');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'Q');
                AppendByte(ref platform, ref writer, (byte)'U');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.If:
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'W');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'L');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'Q');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'K');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'G');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'K');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'G');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'K');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'V');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'L');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'X');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'K');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'Q');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Run:
                AppendByte(ref platform, ref writer, (byte)'D');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'C');
                AppendByte(ref platform, ref writer, (byte)'H');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'Q');
                AppendByte(ref platform, ref writer, (byte)'U');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'C');
                AppendByte(ref platform, ref writer, (byte)'K');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'K');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'P');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'K');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'C');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'D');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Resident:
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'L');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'L');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'K');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'V');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'D');
                AppendByte(ref platform, ref writer, (byte)'D');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'P');
                AppendByte(ref platform, ref writer, (byte)'L');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'C');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'P');
                AppendByte(ref platform, ref writer, (byte)'U');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'=');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'C');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'Y');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'D');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.NewShell:
                AppendByte(ref platform, ref writer, (byte)'W');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'D');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'W');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, 0);
                return;
            case ReadArgsCommandTemplate.Echo:
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'A');
                AppendByte(ref platform, ref writer, (byte)'G');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'M');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'L');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'F');
                AppendByte(ref platform, ref writer, (byte)'I');
                AppendByte(ref platform, ref writer, (byte)'R');
                AppendByte(ref platform, ref writer, (byte)'S');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'K');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'L');
                AppendByte(ref platform, ref writer, (byte)'E');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'K');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'N');
                AppendByte(ref platform, ref writer, (byte)',');
                AppendByte(ref platform, ref writer, (byte)'T');
                AppendByte(ref platform, ref writer, (byte)'O');
                AppendByte(ref platform, ref writer, (byte)'/');
                AppendByte(ref platform, ref writer, (byte)'K');
                AppendByte(ref platform, ref writer, 0);
                return;
        }
    }

    private static void AppendByte<TPlatform>(
        ref TPlatform platform,
        ref ShellReadArgsTemplateWriter writer,
        byte value)
        where TPlatform : struct, IShellPlatform
    {
        // The caller has validated the complete template and backing span.
        platform.WriteUInt8(writer.Buffer, (int)writer.Position, value);
        writer.Position++;
    }

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
