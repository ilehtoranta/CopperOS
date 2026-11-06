using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Shell;

/// <summary>Shell diagnostics using the public DOS fault catalogue.</summary>
internal static class PublicShellFaultServices
{
    private const uint BufferSize = 384;

    public static bool WriteFaults<TMemory>(ref TMemory memory, APTR dos, BPTR output,
        APTR codes, uint count) where TMemory : struct, IShellRunnerMemory
    {
        if (output.IsNull) return Fail(dos, DOS.Error.InvalidLock);
        if (codes.IsNull || (codes.Raw & 1) != 0 || count == 0 || count > FaultCommand.MaximumErrorCodes ||
            codes.Raw > uint.MaxValue - count * 4 || !memory.IsMapped(codes, count * 4))
            return Fail(dos, DOS.Error.ObjectWrongType);
        var buffer = memory.AllocateRunnerMemory(BufferSize);
        if (buffer.IsNull || buffer.Raw > uint.MaxValue - BufferSize || !memory.IsMapped(buffer, BufferSize))
        {
            if (buffer.IsNotNull) memory.FreeRunnerMemory(buffer, BufferSize);
            return Fail(dos, DOS.Error.NoFreeStore);
        }
        var success = true;
        for (var index = 0u; index < count && success; index++)
        {
            var code = unchecked((int)memory.ReadUInt32(codes, (int)(index * 4)));
            var prefix = Text("Fault ");
            var length = 0u;
            while (memory.ReadUInt8(prefix, (int)length) != 0)
            {
                memory.WriteUInt8(buffer, (int)length, memory.ReadUInt8(prefix, (int)length));
                length++;
            }
            var number = code < 0 ? unchecked((uint)(-(code + 1)) + 1u) : (uint)code;
            if (code < 0) memory.WriteUInt8(buffer, (int)length++, (byte)'-');
            var divisor = 1u;
            while (divisor <= number / 10u && divisor <= uint.MaxValue / 10u) divisor *= 10u;
            while (divisor != 0)
            {
                var digit = number / divisor;
                memory.WriteUInt8(buffer, (int)length++, (byte)('0' + digit));
                number %= divisor; divisor /= 10u;
            }
            memory.WriteUInt8(buffer, (int)length++, (byte)':');
            memory.WriteUInt8(buffer, (int)length++, (byte)' ');
            var message = APTR.FromPointer(buffer.Raw + length);
            memory.WriteUInt8(message, 0, 0);
            _ = PublicShellFaultVectors.Fault(dos, code, APTR.Null, message, (int)(BufferSize - length));
            var first = memory.ReadUInt8(message);
            if (first >= (byte)'A' && first <= (byte)'Z') memory.WriteUInt8(message, 0, (byte)(first + 32));
            while (length + 1 < BufferSize && memory.ReadUInt8(buffer, (int)length) != 0) length++;
            memory.WriteUInt8(buffer, (int)length++, (byte)'\n');
            success = PublicShellFileServices.Write(dos, output, buffer, length) == (int)length;
        }
        var error = success ? 0 : PublicShellFileServices.GetError(dos);
        if (!success && error == 0) error = (int)DOS.Error.ObjectWrongType;
        memory.FreeRunnerMemory(buffer, BufferSize);
        PublicShellFileServices.SetError(dos, (DOS.Error)error);
        return success;
    }

    public static bool WriteWhy<TMemory>(ref TMemory memory, APTR dos, BPTR output,
        APTR cli) where TMemory : struct, IShellRunnerMemory
    {
        if (output.IsNull) return Fail(dos, DOS.Error.InvalidLock);
        if (cli.IsNull || !DosCommandLineInterfaceCodec.IsMapped(ref memory, cli))
            return Fail(dos, DOS.Error.ObjectWrongType);
        var code = DosCommandLineInterfaceCodec.Read(ref memory, cli).Result2;
        var buffer = memory.AllocateRunnerMemory(BufferSize);
        if (buffer.IsNull || buffer.Raw > uint.MaxValue - BufferSize || !memory.IsMapped(buffer, BufferSize))
        {
            if (buffer.IsNotNull) memory.FreeRunnerMemory(buffer, BufferSize);
            return Fail(dos, DOS.Error.NoFreeStore);
        }
        var length = 0u;
        var literal = Text("The last command did not set a return code\n");
        if (code != 0) literal = Text("Last command failed because : ");
        while (memory.ReadUInt8(literal, (int)length) != 0)
        {
            memory.WriteUInt8(buffer, (int)length, memory.ReadUInt8(literal, (int)length));
            length++;
        }
        if (code != 0)
        {
            var message = APTR.FromPointer(buffer.Raw + length);
            memory.WriteUInt8(message, 0, 0);
            _ = PublicShellFaultVectors.Fault(dos, code, APTR.Null, message, (int)(BufferSize - length));
            var first = memory.ReadUInt8(message);
            if (first >= (byte)'A' && first <= (byte)'Z') memory.WriteUInt8(message, 0, (byte)(first + 32));
            while (length + 1 < BufferSize && memory.ReadUInt8(buffer, (int)length) != 0) length++;
            memory.WriteUInt8(buffer, (int)length++, (byte)'\n');
        }
        var success = PublicShellFileServices.Write(dos, output, buffer, length) == (int)length;
        var error = success ? code : PublicShellFileServices.GetError(dos);
        if (!success && error == 0) error = (int)DOS.Error.ObjectWrongType;
        memory.FreeRunnerMemory(buffer, BufferSize);
        PublicShellFileServices.SetError(dos, (DOS.Error)error);
        return success;
    }

    private static bool Fail(APTR dos, DOS.Error error)
    {
        PublicShellFileServices.SetError(dos, error);
        return false;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}

[AmigaLibrary("dos.library::shell-fault", AmigaLibraryBasePolicy.CallerProvided)]
internal static class PublicShellFaultVectors
{
    [AmigaLvo(DosLvo.Fault)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Fault([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] int code, [M68kRegister(M68kRegister.D2)] APTR header,
        [M68kRegister(M68kRegister.D3)] APTR buffer, [M68kRegister(M68kRegister.D4)] int size);
}
