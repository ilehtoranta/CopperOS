using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Shell;

/// <summary>Public DOS file operations used by the Shell.</summary>
public static class PublicShellFileServices
{
    public static int Read(APTR dos, BPTR file, APTR buffer, uint length) =>
        PublicShellFileVectors.Read(dos, file, buffer, unchecked((int)length));
    public static int Write(APTR dos, BPTR file, APTR buffer, uint length) =>
        PublicShellFileVectors.Write(dos, file, buffer, unchecked((int)length));
    public static int WriteByte(APTR dos, BPTR file, byte value) =>
        PublicShellFileVectors.FPutC(dos, file, value);
    public static int Flush(APTR dos, BPTR file) => PublicShellFileVectors.Flush(dos, file);
    public static int IsInteractive(APTR dos, BPTR file) => PublicShellFileVectors.IsInteractive(dos, file);
    public static BPTR Input(APTR dos) => PublicShellFileVectors.Input(dos);
    public static BPTR Output(APTR dos) => PublicShellFileVectors.Output(dos);
    public static int GetError(APTR dos) => PublicShellFileVectors.IoErr(dos);
    public static void SetError(APTR dos, DOS.Error error) => _ = PublicShellFileVectors.SetIoErr(dos, (int)error);
    public static int Seek(APTR dos, BPTR file, int position, int mode) =>
        PublicShellFileVectors.Seek(dos, file, position, mode);
    public static APTR ReadString(APTR dos, BPTR file, APTR buffer, int size) =>
        PublicShellFileVectors.FGets(dos, file, buffer, size);
    public static bool ReadLine(APTR dos, BPTR file, APTR buffer, uint capacity,
        out uint length, out uint endOfFile)
    {
        length = 0; endOfFile = 0;
        if (buffer.IsNull || capacity < 2 || capacity > 4096 || buffer.Raw > uint.MaxValue - capacity)
        {
            _ = PublicShellFileVectors.SetIoErr(dos, (int)DOS.Error.ObjectWrongType);
            return false;
        }
        // An EOF indication must not be confused with an earlier DOS error.
        _ = PublicShellFileVectors.SetIoErr(dos, 0);
        if (PublicShellFileVectors.FGets(dos, file, buffer, (int)capacity).IsNull)
        {
            if (PublicShellFileVectors.IoErr(dos) != 0) return false;
            endOfFile = 1;
            return true;
        }
        while (length + 1 < capacity && APTR.ReadUInt8(buffer, (int)length) != 0) length++;
        if (length != 0 && APTR.ReadUInt8(buffer, (int)length - 1) == (byte)'\n')
        {
            length--;
            if (length != 0 && APTR.ReadUInt8(buffer, (int)length - 1) == (byte)'\r') length--;
        }
        APTR.WriteUInt8(buffer, (int)length, 0);
        return true;
    }
    public static BPTR OpenInput(APTR dos, APTR name, uint length)
    {
        if (!ValidName(name, length))
        {
            _ = PublicShellFileVectors.SetIoErr(dos, (int)DOS.Error.BadStreamName);
            return BPTR.Null;
        }
        return PublicShellFileVectors.Open(dos, name, (int)DOS.FileMode.OldFile);
    }
    /// <summary>Best-effort buffering for a script handle owned by the Shell.</summary>
    public static void BufferOwnedScriptInput(APTR dos, BPTR file)
    {
        var previousError = GetError(dos);
        if (IsInteractive(dos, file) == 0)
            _ = PublicShellFileVectors.SetVBuf(dos, file, APTR.Null, DosBufferMode.Full, 1024);
        SetError(dos, (DOS.Error)previousError);
    }
    public static BPTR OpenOutput(APTR dos, APTR name, uint length, uint append)
    {
        if (append > 1 || !ValidName(name, length))
        {
            _ = PublicShellFileVectors.SetIoErr(dos, (int)DOS.Error.BadStreamName);
            return BPTR.Null;
        }
        var output = PublicShellFileVectors.Open(dos, name,
            append == 0 ? (int)DOS.FileMode.NewFile : (int)DOS.FileMode.ReadWrite);
        if (output.IsNull && append != 0)
            output = PublicShellFileVectors.Open(dos, name, (int)DOS.FileMode.NewFile);
        if (output.IsNotNull && append != 0 && PublicShellFileVectors.Seek(dos, output, 0, 1) < 0)
        {
            _ = PublicShellFileVectors.Close(dos, output);
            return BPTR.Null;
        }
        return output;
    }
    public static int Close(APTR dos, BPTR file) => PublicShellFileVectors.Close(dos, file);
    public static int Delete(APTR dos, APTR name) => PublicShellFileVectors.DeleteFile(dos, name);
    public static BPTR Lock(APTR dos, APTR name) => PublicShellFileVectors.Lock(dos, name, -2);
    public static void UnLock(APTR dos, BPTR directory) => PublicShellFileVectors.UnLock(dos, directory);

    private static bool ValidName(APTR name, uint length) =>
        name.IsNotNull && length != 0 && length < uint.MaxValue &&
        name.Raw <= uint.MaxValue - length - 1 &&
        APTR.ReadUInt8(name, unchecked((int)length)) == 0;
}

[AmigaLibrary("dos.library::shell-file", AmigaLibraryBasePolicy.CallerProvided)]
internal static class PublicShellFileVectors
{
    [AmigaLvo(DosLvo.Flush)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Flush([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file);
    [AmigaLvo(DosLvo.Open)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR Open([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name, [M68kRegister(M68kRegister.D2)] int mode);
    [AmigaLvo(DosLvo.Close)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Close([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file);
    [AmigaLvo(DosLvo.Read)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Read([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file, [M68kRegister(M68kRegister.D2)] APTR buffer,
        [M68kRegister(M68kRegister.D3)] int length);
    [AmigaLvo(DosLvo.SetVBuf)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetVBuf([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file, [M68kRegister(M68kRegister.D2)] APTR buffer,
        [M68kRegister(M68kRegister.D3)] DosBufferMode mode, [M68kRegister(M68kRegister.D4)] int size);
    [AmigaLvo(DosLvo.Write)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Write([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file, [M68kRegister(M68kRegister.D2)] APTR buffer,
        [M68kRegister(M68kRegister.D3)] int length);
    [AmigaLvo(DosLvo.FPutC)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int FPutC([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file, [M68kRegister(M68kRegister.D2)] int value);
    [AmigaLvo(DosLvo.FGetC)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int FGetC([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file);
    [AmigaLvo(DosLvo.FGets)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR FGets([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file, [M68kRegister(M68kRegister.D2)] APTR buffer,
        [M68kRegister(M68kRegister.D3)] int size);
    [AmigaLvo(DosLvo.IsInteractive)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int IsInteractive([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file);
    [AmigaLvo(DosLvo.Input)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR Input([M68kRegister(M68kRegister.A6)] APTR dos);
    [AmigaLvo(DosLvo.Output)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR Output([M68kRegister(M68kRegister.A6)] APTR dos);
    [AmigaLvo(DosLvo.IoErr)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int IoErr([M68kRegister(M68kRegister.A6)] APTR dos);
    [AmigaLvo(DosLvo.Seek)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Seek([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file, [M68kRegister(M68kRegister.D2)] int offset,
        [M68kRegister(M68kRegister.D3)] int mode);
    [AmigaLvo(DosLvo.SetIoErr)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetIoErr([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] int error);
    [AmigaLvo(DosLvo.DeleteFile)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int DeleteFile([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name);
    [AmigaLvo(DosLvo.Lock)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR Lock([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name, [M68kRegister(M68kRegister.D2)] int mode);
    [AmigaLvo(DosLvo.UnLock)]
    internal static extern void UnLock([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR directory);
}
