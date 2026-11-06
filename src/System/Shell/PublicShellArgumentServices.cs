using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Shell;

/// <summary>Runs Shell argument parsing through public DOS and Exec services.</summary>
public static class PublicShellArgumentServices
{
    private const uint HeaderSize = 16;
    private const uint Magic = 0x53415247; // SARG
    private const uint TokenBytes = 256;

    /// <summary>Consumes an already supplied command prefix through DOS buffered input.</summary>
    public static bool ConsumeInputPrefix(APTR dos, BPTR input, APTR source, uint length)
    {
        if (dos.IsNull || (length != 0 && (input.IsNull || source.IsNull ||
            source.Raw > uint.MaxValue - length)))
        {
            if (dos.IsNotNull) PublicShellFileServices.SetError(dos, DOS.Error.ObjectWrongType);
            return false;
        }
        var previousError = PublicShellFileServices.GetError(dos);
        for (var i = 0u; i < length; i++)
        {
            var value = PublicShellFileVectors.FGetC(dos, input);
            if (value < 0 || value != APTR.ReadUInt8(source, (int)i))
            {
                if (value >= 0 || PublicShellFileServices.GetError(dos) == 0)
                    PublicShellFileServices.SetError(dos, DOS.Error.ObjectWrongType);
                return false;
            }
        }
        PublicShellFileServices.SetError(dos, (DOS.Error)previousError);
        return true;
    }

    /// <summary>Returns a Shell-owned opaque handle; results remain valid until Free.</summary>
    public static APTR Read(APTR dos, APTR source, uint length, APTR template, APTR results)
    {
        if (dos.IsNull) return APTR.Null;
        if (length > int.MaxValue - 1 || (length != 0 &&
            (source.IsNull || source.Raw > uint.MaxValue - length)) || template.IsNull || results.IsNull)
        {
            _ = PublicShellArgumentVectors.SetIoErr(dos, (int)DOS.Error.ObjectWrongType);
            return APTR.Null;
        }
        var incomingError = PublicShellArgumentVectors.IoErr(dos);
        var size = HeaderSize + length + 2;
        var owner = APTR.FromPointer(global::Amiga.Exec.AllocMem(size,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
        if (owner.IsNull)
        {
            _ = PublicShellArgumentVectors.SetIoErr(dos, (int)DOS.Error.NoFreeStore);
            return APTR.Null;
        }
        var arguments = PublicShellArgumentVectors.AllocDosObject(dos, (uint)DosObjectType.RdArgs, APTR.Null);
        if (arguments.IsNull)
        {
            global::Amiga.Exec.FreeMem(owner.Raw, size);
            _ = PublicShellArgumentVectors.SetIoErr(dos, (int)DOS.Error.NoFreeStore);
            return APTR.Null;
        }
        var text = APTR.FromPointer(owner.Raw + HeaderSize);
        for (var i = 0u; i < length; i++)
            APTR.WriteUInt8(text, (int)i, APTR.ReadUInt8(source, (int)i));
        var inputLength = length;
        if (length == 0 || APTR.ReadUInt8(text, (int)length - 1) != (byte)'\n')
            APTR.WriteUInt8(text, (int)inputLength++, (byte)'\n');
        APTR.WriteUInt32(arguments, DosLayout.RDArgs.Source + DosLayout.CSource.Buffer, text.Raw);
        APTR.WriteUInt32(arguments, DosLayout.RDArgs.Source + DosLayout.CSource.Length, inputLength);
        APTR.WriteUInt32(arguments, DosLayout.RDArgs.Source + DosLayout.CSource.CurrentCharacter, 0);
        _ = PublicShellArgumentVectors.SetIoErr(dos, incomingError);
        var parsed = PublicShellArgumentVectors.ReadArgs(dos, template, results, arguments);
        if (parsed.IsNull)
        {
            var error = PublicShellArgumentVectors.IoErr(dos);
            PublicShellArgumentVectors.FreeArgs(dos, arguments);
            PublicShellArgumentVectors.FreeDosObject(dos, (uint)DosObjectType.RdArgs, arguments);
            global::Amiga.Exec.FreeMem(owner.Raw, size);
            _ = PublicShellArgumentVectors.SetIoErr(dos, error);
            return APTR.Null;
        }
        APTR.WriteUInt32(owner, 0, Magic);
        APTR.WriteUInt32(owner, 4, arguments.Raw);
        APTR.WriteUInt32(owner, 8, size);
        return owner;
    }

    public static void Free(APTR dos, APTR owner)
    {
        if (dos.IsNull || owner.IsNull || APTR.ReadUInt32(owner, 0) != Magic) return;
        var error = PublicShellArgumentVectors.IoErr(dos);
        var arguments = APTR.FromPointer(APTR.ReadUInt32(owner, 4));
        var size = APTR.ReadUInt32(owner, 8);
        APTR.WriteUInt32(owner, 0, 0);
        PublicShellArgumentVectors.FreeArgs(dos, arguments);
        PublicShellArgumentVectors.FreeDosObject(dos, (uint)DosObjectType.RdArgs, arguments);
        global::Amiga.Exec.FreeMem(owner.Raw, size);
        _ = PublicShellArgumentVectors.SetIoErr(dos, error);
    }

    /// <summary>Returns the raw cursor after the first public DOS ReadItem.</summary>
    public static bool ReadFirstItem(APTR dos, APTR source, uint length, out uint consumed)
    {
        consumed = 0;
        if (dos.IsNull) return false;
        if (length > int.MaxValue || (length != 0 &&
            (source.IsNull || source.Raw > uint.MaxValue - length)))
        {
            _ = PublicShellArgumentVectors.SetIoErr(dos, (int)DOS.Error.ObjectWrongType);
            return false;
        }
        const uint size = CSource.Size + TokenBytes + 1;
        var memory = APTR.FromPointer(global::Amiga.Exec.AllocMem(size,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
        if (memory.IsNull)
        {
            _ = PublicShellArgumentVectors.SetIoErr(dos, (int)DOS.Error.NoFreeStore);
            return false;
        }
        var buffer = APTR.FromPointer(memory.Raw + CSource.Size);
        var empty = APTR.FromPointer(memory.Raw + CSource.Size + TokenBytes);
        var sourceRaw = source.Raw;
        if (length == 0) sourceRaw = empty.Raw;
        APTR.WriteUInt32(memory, DosLayout.CSource.Buffer, sourceRaw);
        APTR.WriteUInt32(memory, DosLayout.CSource.Length, length);
        var kind = PublicShellArgumentVectors.ReadItem(dos, buffer, (int)TokenBytes, memory);
        var cursor = APTR.ReadUInt32(memory, DosLayout.CSource.CurrentCharacter);
        global::Amiga.Exec.FreeMem(memory.Raw, size);
        if (cursor > length)
        {
            _ = PublicShellArgumentVectors.SetIoErr(dos, (int)DOS.Error.ObjectWrongType);
            return false;
        }
        consumed = cursor;
        return kind == (int)DOS.ItemKind.Unquoted || kind == (int)DOS.ItemKind.Quoted;
    }
}

[AmigaLibrary("dos.library::shell-arguments", AmigaLibraryBasePolicy.CallerProvided)]
internal static class PublicShellArgumentVectors
{
    [AmigaLvo(DosLvo.AllocDosObject)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR AllocDosObject([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] uint type, [M68kRegister(M68kRegister.D2)] APTR tags);
    [AmigaLvo(DosLvo.FreeDosObject)]
    internal static extern void FreeDosObject([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] uint type, [M68kRegister(M68kRegister.D2)] APTR value);
    [AmigaLvo(DosLvo.ReadArgs)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR ReadArgs([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR template, [M68kRegister(M68kRegister.D2)] APTR results,
        [M68kRegister(M68kRegister.D3)] APTR arguments);
    [AmigaLvo(DosLvo.FreeArgs)]
    internal static extern void FreeArgs([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR arguments);
    [AmigaLvo(DosLvo.ReadItem)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int ReadItem([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR buffer, [M68kRegister(M68kRegister.D2)] int size,
        [M68kRegister(M68kRegister.D3)] APTR source);
    [AmigaLvo(DosLvo.IoErr)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int IoErr([M68kRegister(M68kRegister.A6)] APTR dos);
    [AmigaLvo(DosLvo.SetIoErr)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetIoErr([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] int error);
}
