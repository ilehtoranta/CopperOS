using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Shell;

/// <summary>Uses DOS's process-owned local variables and aliases.</summary>
internal static class PublicShellVariableServices
{
    private const uint LocalBinary = (uint)(GlobalVariableFlags.LocalOnly | GlobalVariableFlags.BinaryVariable);
    private const uint GlobalBinary = (uint)(GlobalVariableFlags.GlobalOnly | GlobalVariableFlags.BinaryVariable);

    public static bool SetGlobal<TMemory>(ref TMemory memory, APTR dos, APTR name,
        uint nameLength, APTR value, uint valueLength, uint save)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (save > 1 || !ValidString(ref memory, name, nameLength, false) ||
            valueLength > int.MaxValue || !ValidString(ref memory, value, valueLength, true))
            return Fail(dos, DOS.Error.ObjectWrongType);
        var actualValue = value;
        if (actualValue.IsNull) actualValue = APTR.FromPointer(name.Raw + nameLength);
        var flags = GlobalBinary | (save != 0 ? (uint)GlobalVariableFlags.SaveVariable : 0u);
        if (PublicShellVariableVectors.SetVar(dos, name, actualValue, (int)valueLength, flags) == 0) return false;
        PublicShellFileServices.SetError(dos, DOS.Error.None);
        return true;
    }

    public static bool GetGlobal<TMemory>(ref TMemory memory, APTR dos, APTR name,
        uint nameLength, APTR destination, uint capacity, out uint valueLength)
        where TMemory : struct, IAmigaGuestMemory =>
        GetValue(ref memory, dos, name, nameLength, destination, capacity, GlobalBinary, out valueLength);

    public static bool RemoveGlobal<TMemory>(ref TMemory memory, APTR dos, APTR name,
        uint nameLength, uint save) where TMemory : struct, IAmigaGuestMemory
    {
        if (save > 1 || !ValidString(ref memory, name, nameLength, false))
            return Fail(dos, DOS.Error.ObjectWrongType);
        var flags = GlobalBinary | (save != 0 ? (uint)GlobalVariableFlags.SaveVariable : 0u);
        if (PublicShellVariableVectors.DeleteVar(dos, name, flags) == 0) return false;
        PublicShellFileServices.SetError(dos, DOS.Error.None);
        return true;
    }

    public static bool Set<TMemory>(ref TMemory memory, APTR dos, APTR cli,
        APTR name, uint nameLength, APTR value, uint valueLength, bool alias)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (!ValidCurrentCli(ref memory, cli) || !ValidString(ref memory, name, nameLength, false) ||
            valueLength > int.MaxValue || !ValidString(ref memory, value, valueLength, true))
            return Fail(dos, DOS.Error.ObjectWrongType);
        var actualValue = value;
        if (actualValue.IsNull) actualValue = APTR.FromPointer(name.Raw + nameLength);
        var flags = LocalBinary | (alias ? (uint)LocalVariableType.Alias : 0u);
        if (PublicShellVariableVectors.SetVar(dos, name, actualValue, (int)valueLength, flags) == 0) return false;
        PublicShellFileServices.SetError(dos, DOS.Error.None);
        return true;
    }

    public static bool Get<TMemory>(ref TMemory memory, APTR dos, APTR cli,
        APTR name, uint nameLength, APTR destination, uint capacity, bool alias,
        out uint valueLength) where TMemory : struct, IAmigaGuestMemory
    {
        if (!ValidCurrentCli(ref memory, cli))
        {
            valueLength = 0;
            return Fail(dos, DOS.Error.ObjectWrongType);
        }
        var flags = LocalBinary | (alias ? (uint)LocalVariableType.Alias : 0u);
        return GetValue(ref memory, dos, name, nameLength, destination, capacity, flags, out valueLength);
    }

    private static bool GetValue<TMemory>(ref TMemory memory, APTR dos, APTR name,
        uint nameLength, APTR destination, uint capacity, uint flags, out uint valueLength)
        where TMemory : struct, IAmigaGuestMemory
    {
        valueLength = 0;
        if (!ValidString(ref memory, name, nameLength, false) ||
            destination.IsNull || capacity == 0 || capacity > int.MaxValue ||
            destination.Raw > uint.MaxValue - capacity || !memory.IsMapped(destination, capacity))
            return Fail(dos, DOS.Error.ObjectWrongType);
        var copied = PublicShellVariableVectors.GetVar(dos, name, destination, (int)capacity, flags);
        var error = PublicShellFileServices.GetError(dos);
        if (copied < 0)
        {
            if (error == (int)DOS.Error.ObjectNotFound) memory.WriteUInt8(destination, 0, 0);
            return false;
        }
        valueLength = unchecked((uint)error);
        if (valueLength >= capacity) return Fail(dos, DOS.Error.BufferOverflow);
        PublicShellFileServices.SetError(dos, DOS.Error.None);
        return true;
    }

    public static bool Remove<TMemory>(ref TMemory memory, APTR dos, APTR cli,
        APTR name, uint nameLength, bool alias) where TMemory : struct, IAmigaGuestMemory
    {
        if (!ValidCurrentCli(ref memory, cli) || !ValidString(ref memory, name, nameLength, false))
            return Fail(dos, DOS.Error.ObjectWrongType);
        var flags = LocalBinary | (alias ? (uint)LocalVariableType.Alias : 0u);
        if (PublicShellVariableVectors.DeleteVar(dos, name, flags) == 0) return false;
        PublicShellFileServices.SetError(dos, DOS.Error.None);
        return true;
    }

    public static bool ExpandAlias<TPlatform>(ref TPlatform platform, APTR dos, APTR cli,
        APTR source, uint sourceLength, APTR destination, uint capacity,
        out uint expanded, out uint expandedLength) where TPlatform : struct, IShellRunnerMemory
    {
        expanded = 0; expandedLength = 0;
        if (!ValidCurrentCli(ref platform, cli) || source.IsNull || sourceLength == 0 ||
            sourceLength > int.MaxValue || source.Raw > uint.MaxValue - sourceLength ||
            !platform.IsMapped(source, sourceLength) || destination.IsNull || capacity == 0 ||
            capacity > int.MaxValue || destination.Raw > uint.MaxValue - capacity ||
            !platform.IsMapped(destination, capacity)) return Fail(dos, DOS.Error.ObjectWrongType);
        var tokenLength = 0u;
        while (tokenLength < sourceLength && !IsSpace(platform.ReadUInt8(source, (int)tokenLength))) tokenLength++;
        if (tokenLength == 0) return true;
        var token = platform.AllocateRunnerMemory(tokenLength + 1);
        if (token.IsNull || token.Raw > uint.MaxValue - tokenLength - 1 ||
            !platform.IsMapped(token, tokenLength + 1))
        {
            if (token.IsNotNull) platform.FreeRunnerMemory(token, tokenLength + 1);
            return Fail(dos, DOS.Error.NoFreeStore);
        }
        for (var i = 0u; i < tokenLength; i++) platform.WriteUInt8(token, (int)i, platform.ReadUInt8(source, (int)i));
        platform.WriteUInt8(token, (int)tokenLength, 0);
        var previousError = PublicShellFileServices.GetError(dos);
        var entry = PublicShellVariableVectors.FindVar(dos, token, (uint)LocalVariableType.Alias);
        platform.FreeRunnerMemory(token, tokenLength + 1);
        if (entry.IsNull)
        {
            PublicShellFileServices.SetError(dos, (DOS.Error)previousError);
            return true;
        }
        if (!DosLocalVarCodec.IsMapped(ref platform, entry)) return Fail(dos, DOS.Error.ObjectWrongType);
        var variable = DosLocalVarCodec.Read(ref platform, entry);
        if (variable.Length > int.MaxValue || (variable.Length != 0 &&
            (variable.Value.IsNull || variable.Value.Raw > uint.MaxValue - variable.Length ||
             !platform.IsMapped(variable.Value, variable.Length)))) return Fail(dos, DOS.Error.ObjectWrongType);
        var remainder = tokenLength;
        while (remainder < sourceLength && IsSpace(platform.ReadUInt8(source, (int)remainder))) remainder++;
        var tail = APTR.FromPointer(source.Raw + remainder);
        var tailLength = sourceLength - remainder;
        var cursor = 0u;
        var substituted = false;
        for (var position = 0u; position < variable.Length;)
        {
            var ch = platform.ReadUInt8(variable.Value, (int)position);
            var count = ch == (byte)'*' && position + 1 < variable.Length ? 2u : 1u;
            if (ch == (byte)'[')
            {
                var end = position + 1;
                while (end < variable.Length && IsSpace(platform.ReadUInt8(variable.Value, (int)end))) end++;
                if (end < variable.Length && platform.ReadUInt8(variable.Value, (int)end) == (byte)']')
                {
                    if (!Append(ref platform, dos, tail, tailLength, destination, capacity, ref cursor)) return false;
                    substituted = true;
                    position = end + 1;
                    continue;
                }
            }
            if (!Append(ref platform, dos, APTR.FromPointer(variable.Value.Raw + position), count,
                destination, capacity, ref cursor)) return false;
            position += count;
        }
        if (!substituted && tailLength != 0)
        {
            if (cursor + 1 >= capacity) return Fail(dos, DOS.Error.BufferOverflow);
            platform.WriteUInt8(destination, (int)cursor++, (byte)' ');
            if (!Append(ref platform, dos, tail, tailLength, destination, capacity, ref cursor)) return false;
        }
        platform.WriteUInt8(destination, (int)cursor, 0);
        expanded = 1; expandedLength = cursor;
        PublicShellFileServices.SetError(dos, DOS.Error.None);
        return true;
    }

    private static bool Append<TMemory>(ref TMemory memory, APTR dos, APTR source, uint length,
        APTR destination, uint capacity, ref uint cursor) where TMemory : struct, IAmigaGuestMemory
    {
        if (cursor >= capacity || length >= capacity - cursor) return Fail(dos, DOS.Error.BufferOverflow);
        for (var i = 0u; i < length; i++)
            memory.WriteUInt8(destination, (int)(cursor + i), memory.ReadUInt8(source, (int)i));
        cursor += length;
        return true;
    }

    private static bool IsSpace(byte ch) => ch is (byte)' ' or (byte)'\t';

    private static bool ValidCurrentCli<TMemory>(ref TMemory memory, APTR cli)
        where TMemory : struct, IAmigaGuestMemory
    {
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        return cli.IsNotNull && task.IsNotNull && DosProcessCodec.IsMapped(ref memory, task) &&
            DosCommandLineInterfaceCodec.IsMapped(ref memory, cli) &&
            DosProcessCodec.ReadCommandLineInterface(ref memory, task).Address == cli;
    }

    private static bool ValidString<TMemory>(ref TMemory memory, APTR text, uint length, bool emptyAllowed)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (text.IsNull) return emptyAllowed && length == 0;
        return (emptyAllowed || length != 0) && length <= int.MaxValue &&
            text.Raw <= uint.MaxValue - length - 1 && memory.IsMapped(text, length + 1) &&
            memory.ReadUInt8(text, (int)length) == 0;
    }

    private static bool Fail(APTR dos, DOS.Error error)
    {
        PublicShellFileServices.SetError(dos, error);
        return false;
    }
}

[AmigaLibrary("dos.library::shell-variables", AmigaLibraryBasePolicy.CallerProvided)]
internal static class PublicShellVariableVectors
{
    [AmigaLvo(DosLvo.SetVar)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetVar([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name, [M68kRegister(M68kRegister.D2)] APTR value,
        [M68kRegister(M68kRegister.D3)] int length, [M68kRegister(M68kRegister.D4)] uint flags);
    [AmigaLvo(DosLvo.GetVar)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int GetVar([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name, [M68kRegister(M68kRegister.D2)] APTR value,
        [M68kRegister(M68kRegister.D3)] int capacity, [M68kRegister(M68kRegister.D4)] uint flags);
    [AmigaLvo(DosLvo.DeleteVar)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int DeleteVar([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name, [M68kRegister(M68kRegister.D2)] uint flags);
    [AmigaLvo(DosLvo.FindVar)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR FindVar([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name, [M68kRegister(M68kRegister.D2)] uint type);
}
