using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Shell;

/// <summary>Reads public CLI BSTRs and renders Shell prompt substitutions.</summary>
internal static class PublicShellPromptServices
{
    private struct Context
    {
        public APTR Prompt, Directory;
        public uint PromptLength, DirectoryLength;
        public int ShellNumber, ReturnCode;
    }

    public static bool Set<TMemory>(ref TMemory memory, APTR dos, APTR cli,
        APTR value, uint length, uint reset) where TMemory : struct, IShellRunnerMemory
    {
        if (reset > 1 || !IsCurrentCli(ref memory, cli)) return Fail(dos, DOS.Error.ObjectWrongType);
        var actual = value;
        var copy = APTR.Null;
        if (reset != 0) actual = Text("%N.%S>");
        else
        {
            var size = length == 0 ? 1u : length;
            if (value.IsNull || length > 255 || value.Raw > uint.MaxValue - size ||
                !memory.IsMapped(value, size)) return Fail(dos, DOS.Error.ObjectWrongType);
            for (var i = 0u; i < length; i++)
                if (memory.ReadUInt8(value, (int)i) == 0) return Fail(dos, DOS.Error.ObjectWrongType);
            copy = memory.AllocateRunnerMemory(length + 1);
            if (copy.IsNull || copy.Raw > uint.MaxValue - length - 1 || !memory.IsMapped(copy, length + 1))
            {
                if (copy.IsNotNull) memory.FreeRunnerMemory(copy, length + 1);
                return Fail(dos, DOS.Error.NoFreeStore);
            }
            for (var i = 0u; i < length; i++) memory.WriteUInt8(copy, (int)i, memory.ReadUInt8(value, (int)i));
            memory.WriteUInt8(copy, (int)length, 0);
            actual = copy;
        }
        var result = PublicShellPromptVectors.SetPrompt(dos, actual);
        var error = PublicShellFileServices.GetError(dos);
        if (copy.IsNotNull) memory.FreeRunnerMemory(copy, length + 1);
        if (result == 0) { PublicShellFileServices.SetError(dos, (DOS.Error)error); return false; }
        PublicShellFileServices.SetError(dos, DOS.Error.None);
        return true;
    }

    public static bool CopyTemplate<TMemory>(ref TMemory memory, APTR dos, APTR cli,
        APTR destination, uint capacity, out uint length) where TMemory : struct, IAmigaGuestMemory
    {
        length = 0;
        if (destination.IsNull || capacity == 0 || capacity > int.MaxValue ||
            destination.Raw > uint.MaxValue - capacity || !memory.IsMapped(destination, capacity) ||
            !ReadContext(ref memory, cli, out var context)) return Fail(dos, DOS.Error.ObjectWrongType);
        if (context.PromptLength >= capacity) return Fail(dos, DOS.Error.LineTooLong);
        length = context.PromptLength;
        for (var i = 0u; i < length; i++)
            memory.WriteUInt8(destination, (int)i, memory.ReadUInt8(context.Prompt, (int)i + 1));
        memory.WriteUInt8(destination, (int)length, 0);
        PublicShellFileServices.SetError(dos, DOS.Error.None);
        return true;
    }

    public static bool WritePrompt<TMemory>(ref TMemory memory, APTR dos, APTR cli, BPTR output)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (output.IsNull || !ReadContext(ref memory, cli, out var context))
            return Fail(dos, DOS.Error.ObjectWrongType);
        var text = context.Prompt;
        if (text.IsNotNull) text = APTR.FromPointer(text.Raw + 1);
        return WriteSpan(ref memory, dos, output, text, context.PromptLength, in context);
    }

    public static bool WriteLiteral<TMemory>(ref TMemory memory, APTR dos, APTR cli, BPTR output,
        APTR text, uint length) where TMemory : struct, IAmigaGuestMemory
    {
        if (output.IsNull || length > 255 || (length != 0 && (text.IsNull ||
            text.Raw > uint.MaxValue - length || !memory.IsMapped(text, length))) ||
            !ReadContext(ref memory, cli, out var context)) return Fail(dos, DOS.Error.ObjectWrongType);
        return WriteSpan(ref memory, dos, output, text, length, in context);
    }

    private static bool WriteSpan<TMemory>(ref TMemory memory, APTR dos, BPTR output,
        APTR text, uint length, in Context context) where TMemory : struct, IAmigaGuestMemory
    {
        for (var i = 0u; i < length; i++)
            if (memory.ReadUInt8(text, (int)i) == (byte)'`') return Fail(dos, DOS.Error.ActionNotKnown);
        for (var i = 0u; i < length; i++)
        {
            var ch = memory.ReadUInt8(text, (int)i);
            if (ch == (byte)'%' && i + 1 < length)
            {
                var kind = memory.ReadUInt8(text, (int)i + 1);
                if (kind == (byte)'N' || kind == (byte)'R')
                {
                    if (!WriteNumber(dos, output, kind == (byte)'N' ? context.ShellNumber : context.ReturnCode)) return false;
                    i++; continue;
                }
                if (kind == (byte)'S')
                {
                    for (var n = 0u; n < context.DirectoryLength; n++)
                        if (PublicShellFileServices.WriteByte(dos, output, memory.ReadUInt8(context.Directory, (int)n + 1)) < 0) return false;
                    i++; continue;
                }
            }
            if (PublicShellFileServices.WriteByte(dos, output, ch) < 0) return false;
        }
        PublicShellFileServices.SetError(dos, DOS.Error.None);
        return true;
    }

    private static bool WriteNumber(APTR dos, BPTR output, int value)
    {
        var number = value < 0 ? unchecked((uint)(-(value + 1)) + 1u) : (uint)value;
        if (value < 0 && PublicShellFileServices.WriteByte(dos, output, (byte)'-') < 0) return false;
        var divisor = 1u;
        while (divisor <= number / 10u && divisor <= uint.MaxValue / 10u) divisor *= 10u;
        while (divisor != 0)
        {
            var digit = number / divisor;
            if (PublicShellFileServices.WriteByte(dos, output, (byte)('0' + digit)) < 0) return false;
            number %= divisor; divisor /= 10u;
        }
        return true;
    }

    private static bool ReadContext<TMemory>(ref TMemory memory, APTR cli, out Context context)
        where TMemory : struct, IAmigaGuestMemory
    {
        context = default;
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        if (!IsCurrentCli(ref memory, cli) || task.IsNull) return false;
        var value = DosCommandLineInterfaceCodec.Read(ref memory, cli);
        if (!ReadBstr(ref memory, value.Prompt, out var prompt, out var promptLength) ||
            !ReadBstr(ref memory, value.CurrentDirectoryName, out var directory, out var directoryLength)) return false;
        context.Prompt = prompt; context.PromptLength = promptLength;
        context.Directory = directory; context.DirectoryLength = directoryLength;
        context.ShellNumber = DosProcessCodec.ReadTaskNumber(ref memory, task);
        context.ReturnCode = value.ReturnCode;
        return true;
    }

    private static bool IsCurrentCli<TMemory>(ref TMemory memory, APTR cli)
        where TMemory : struct, IAmigaGuestMemory
    {
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        return cli.IsNotNull && task.IsNotNull && DosProcessCodec.IsMapped(ref memory, task) &&
            DosCommandLineInterfaceCodec.IsMapped(ref memory, cli) &&
            DosProcessCodec.ReadCommandLineInterface(ref memory, task).Address == cli;
    }

    private static bool ReadBstr<TMemory>(ref TMemory memory, BPTR pointer, out APTR text, out uint length)
        where TMemory : struct, IAmigaGuestMemory
    {
        text = APTR.Null; length = 0;
        if (pointer.IsNull) return true;
        text = pointer.Address;
        if (text.IsNull || !memory.IsMapped(text, 1)) return false;
        length = memory.ReadUInt8(text);
        return text.Raw <= uint.MaxValue - length - 1 && memory.IsMapped(text, length + 1);
    }

    private static bool Fail(APTR dos, DOS.Error error)
    {
        PublicShellFileServices.SetError(dos, error);
        return false;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}

[AmigaLibrary("dos.library::shell-prompt", AmigaLibraryBasePolicy.CallerProvided)]
internal static class PublicShellPromptVectors
{
    [AmigaLvo(DosLvo.SetPrompt)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetPrompt([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR value);
}
