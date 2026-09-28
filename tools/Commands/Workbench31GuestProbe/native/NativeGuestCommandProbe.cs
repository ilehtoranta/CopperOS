using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Workbench31GuestProbe;

/// <summary>
/// Authored diagnostic CLI command for an original Kickstart/Workbench guest.
/// Publishes only through Exec's public port list and retains its process,
/// HUNK, port, name/record and output until the diagnostic guest is rebooted.
/// This is not a supplied-vector test or an installed CopperOS command.
/// </summary>
public static class NativeGuestCommandProbe
{
    public const string PortName = "CopperOS.CommandProbe.v1";
    public const int RecordBytes = 96;
    public const int HeaderOffset = 32;
    public const int OutputCapacity = 4096;

    // All offsets below are relative to the fixed header at name pointer +32.
    private const int Magic = 0, Version = 4, Token = 8, Stage = 12;
    private const int CommandReturn = 16, PostSystemIoErr = 20, OutputLength = 24, ProbeError = 28;
    private const int Process = 32, OutputBuffer = 36, Capacity = 40, Port = 44;
    private const int Flags = 48, ErrorIoErr = 52, RecordLength = 56;

    private const uint ArgumentsParsed = 1, SystemInvoked = 2, SystemReturned = 4;
    private const uint OutputComplete = 8, OutputTruncated = 16;

    private const uint DosUnavailable = 1, ReadArgsFailed = 2, OutputAllocationFailed = 3;
    private const uint OutputOpenFailed = 4, SystemFailed = 5, SeekFailed = 6;
    private const uint ReadFailed = 7, Truncated = 8, CloseFailed = 9;
    private const uint InputOpenFailed = 10;
    private const uint FlushFailed = 11;
    private const uint SelfPlaceholderInvalid = 12;
    private const uint MaximumCommandLength = 512;

#pragma warning disable CS0649 // Native stack storage accessed through explicit offsets.
    private struct SystemTags
    {
        public uint A, B, C, D, E, F, G, H;

        public static APTR AddressOf(ref SystemTags value) =>
            throw new System.NotSupportedException("SystemTags.AddressOf is lowered by CopperSharp.");
    }
#pragma warning restore CS0649

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var record = Exec.AllocVec(RecordBytes, (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
        if (record.IsNull)
            return DOS.RETURN_FAIL;
        var port = Exec.CreateMsgPort();
        if (port.IsNull)
        {
            Exec.FreeVec(record);
            return DOS.RETURN_FAIL;
        }

        var name = APTR.FromPointer(CString.ToUInt32(CString.FromLiteral(PortName)));
        for (var index = 0; index < 25; index++)
            APTR.WriteUInt8(record, index, APTR.ReadUInt8(name, index));
        var header = APTR.FromPointer(record.Raw + HeaderOffset);
        APTR.WriteUInt32(header, Magic, 0x43505242); // ASCII CPRB.
        APTR.WriteUInt32(header, Version, 1);
        APTR.WriteUInt32(header, CommandReturn, 0xffffffff); // Not returned yet.
        APTR.WriteUInt32(header, Process, Exec.FindTask(CString.FromPointer(0)).Raw);
        APTR.WriteUInt32(header, Capacity, OutputCapacity);
        APTR.WriteUInt32(header, Port, port.Raw);
        APTR.WriteUInt32(header, RecordLength, RecordBytes);
        APTR.WriteUInt32(port, ExecLayout.Node.Name, record.Raw);
        APTR.WriteUInt8(port, ExecLayout.Node.Priority, 0);
        APTR.WriteUInt32(header, Stage, 1); // Published/starting.

        // A diagnostic reboot is required between probes. Never overwrite an
        // earlier capture or publish an ambiguous second marker of this name.
        Exec.Forbid();
        var existing = Exec.FindPort(CString.FromPointer(record.Raw));
        if (existing.IsNotNull)
        {
            Exec.Permit();
            Exec.DeleteMsgPort(port);
            Exec.FreeVec(record);
            return DOS.RETURN_FAIL;
        }
        Exec.AddPort(port);
        Exec.Permit();

        Run(header);
        // Commit terminal status only after all output, result, flags and
        // cleanup error fields are final. Readers reject intermediate stages.
        APTR.WriteUInt32(header, Stage, APTR.ReadUInt32(header, ProbeError) == 0 ? 100u : 200u);
        while (true)
            Exec.Wait(0);
    }

    private static void Run(APTR header)
    {
        var dos = Exec.OpenLibraryRaw("dos.library", 39);
        if (dos.IsNull)
        {
            Fail(header, DosUnavailable, 122);
            return;
        }
        DOS.DOSLibraryBase = dos;
        var arguments = default(NativeCommandArguments);
        if (!NativeCommandArguments.TryRead("COMMAND/A,TOKEN/N/A", 2, out arguments))
        {
            Fail(header, ReadArgsFailed, arguments.IoError);
            arguments.Release();
            Exec.CloseLibrary(dos);
            DOS.DOSLibraryBase = APTR.Null;
            return;
        }

        if (!arguments.TryGetResult(0, out var command) || command == 0 ||
            !arguments.TryGetResult(1, out var tokenPointer) || tokenPointer == 0)
        {
            Fail(header, ReadArgsFailed, 116); // ERROR_REQUIRED_ARG_MISSING.
        }
        else
        {
            APTR.WriteUInt32(header, Token, APTR.ReadUInt32(APTR.FromPointer(tokenPointer), 0));
            SetFlags(header, ArgumentsParsed);
            APTR.WriteUInt32(header, Stage, 2);
            var output = Exec.AllocVec(OutputCapacity, (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
            if (output.IsNull)
                Fail(header, OutputAllocationFailed, 0);
            else
            {
                APTR.WriteUInt32(header, OutputBuffer, output.Raw);
                ExecuteAndCapture(header, CString.FromPointer(command), output);
            }
        }

        arguments.Release();
        Exec.CloseLibrary(dos);
        DOS.DOSLibraryBase = APTR.Null;
    }

    private static void ExecuteAndCapture(APTR header, CString command, APTR output)
    {
        var source = APTR.FromPointer(CString.ToUInt32(command));
        var owner = APTR.FromPointer(APTR.ReadUInt32(header, Process));
        var expanded = ExpandSelfTaskPlaceholder(source, owner,
            out var invalidPlaceholder, out var expansionAllocationFailed);
        if (invalidPlaceholder)
        {
            Fail(header, SelfPlaceholderInvalid, 0);
            return;
        }
        if (expansionAllocationFailed)
        {
            Fail(header, OutputAllocationFailed, 0);
            return;
        }
        var commandPointer = CString.ToUInt32(command);
        if (expanded.IsNotNull)
            commandPointer = expanded.Raw;

        var input = DOS.OpenRaw(CString.FromLiteral("NIL:"), DOS.FileMode.OldFile);
        if (input.IsNull)
        {
            Fail(header, InputOpenFailed, unchecked((int)DOS.IoErr()));
            if (expanded.IsNotNull)
                Exec.FreeVec(expanded);
            return;
        }
        var file = DOS.OpenRaw(CString.FromLiteral("RAM:CopperProbe.Output"), DOS.FileMode.NewFile);
        if (file.IsNull)
        {
            Fail(header, OutputOpenFailed, unchecked((int)DOS.IoErr()));
            DOS.Close(input);
            if (expanded.IsNotNull)
                Exec.FreeVec(expanded);
            return;
        }
        APTR.WriteUInt32(header, Stage, 3);
        // Keep stage 3 observable in passive frame snapshots before exercising
        // a command that changes this long-lived CLI task's priority.
        if (expanded.IsNotNull)
            DOS.Delay(150);
        var storage = default(SystemTags);
        var tags = SystemTags.AddressOf(ref storage);
        APTR.WriteUInt32(tags, 0, (uint)DosSystemTag.Input);
        APTR.WriteUInt32(tags, 4, input.Raw);
        APTR.WriteUInt32(tags, 8, (uint)DosSystemTag.Output);
        APTR.WriteUInt32(tags, 12, file.Raw);
        APTR.WriteUInt32(tags, 16, (uint)DosSystemTag.Asynchronous);
        APTR.WriteUInt32(tags, 20, 0);
        APTR.WriteUInt32(tags, 24, 0); // TAG_DONE.
        APTR.WriteUInt32(tags, 28, 0);
        SetFlags(header, SystemInvoked);
        APTR.WriteUInt32(header, Stage, 4);
        var result = DOS.SystemTagList(CString.FromPointer(commandPointer), tags);
        // This is the caller's immediate post-System IoErr, not a proven copy
        // of the child's pr_Result2. The DOS contract guarantees return code.
        var error = unchecked((int)DOS.IoErr());
        APTR.WriteUInt32(header, CommandReturn, unchecked((uint)result));
        APTR.WriteUInt32(header, PostSystemIoErr, unchecked((uint)error));
        SetFlags(header, SystemReturned);
        APTR.WriteUInt32(header, Stage, 5);
        if (result == -1)
            Fail(header, SystemFailed, error);

        ReadOutput(header, file, output);
        // Synchronous System does not transfer ownership of these handles.
        // Closing output happens after capture and before terminal publication.
        if (DOS.Close(file) == 0)
            Fail(header, CloseFailed, unchecked((int)DOS.IoErr()));
        if (DOS.Close(input) == 0)
            Fail(header, CloseFailed, unchecked((int)DOS.IoErr()));
        if (expanded.IsNotNull)
            Exec.FreeVec(expanded);
    }

    private static APTR ExpandSelfTaskPlaceholder(APTR source, APTR process,
        out bool invalid, out bool allocationFailed)
    {
        invalid = false;
        allocationFailed = false;
        var sourceLength = 0;
        var placeholders = 0u;
        while (sourceLength < (int)MaximumCommandLength)
        {
            if (APTR.ReadUInt8(source, sourceLength) == 0)
                break;
            if (IsSelfTaskPlaceholder(source, sourceLength))
            {
                placeholders++;
                sourceLength += 6;
            }
            else
                sourceLength++;
        }
        if (sourceLength == (int)MaximumCommandLength || placeholders > 1)
        {
            invalid = true;
            return APTR.Null;
        }
        if (placeholders == 0)
            return APTR.Null;

        var taskNumber = APTR.ReadUInt32(process, DosLayout.Process.TaskNumber);
        if (taskNumber == 0 || taskNumber > 0x7fffffff)
        {
            invalid = true;
            return APTR.Null;
        }

        var buffer = Exec.AllocVec(MaximumCommandLength,
            (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
        if (buffer.IsNull)
        {
            allocationFailed = true;
            return APTR.Null;
        }

        var sourceIndex = 0;
        var destinationIndex = 0;
        while (sourceIndex < sourceLength)
        {
            if (IsSelfTaskPlaceholder(source, sourceIndex))
            {
                WriteDecimal(buffer, ref destinationIndex, taskNumber);
                sourceIndex += 6;
            }
            else
            {
                APTR.WriteUInt8(buffer, destinationIndex,
                    APTR.ReadUInt8(source, sourceIndex));
                sourceIndex++;
                destinationIndex++;
            }
        }
        APTR.WriteUInt8(buffer, destinationIndex, 0);
        return buffer;
    }

    private static bool IsSelfTaskPlaceholder(APTR source, int offset) =>
        APTR.ReadUInt8(source, offset) == (byte)'@' &&
        APTR.ReadUInt8(source, offset + 1) == (byte)'S' &&
        APTR.ReadUInt8(source, offset + 2) == (byte)'E' &&
        APTR.ReadUInt8(source, offset + 3) == (byte)'L' &&
        APTR.ReadUInt8(source, offset + 4) == (byte)'F' &&
        APTR.ReadUInt8(source, offset + 5) == (byte)'@';

    private static void WriteDecimal(APTR destination, ref int offset, uint value)
    {
        var remaining = value;
        var started = false;
        WriteDecimalDigit(destination, ref offset, ref remaining, 1000000000u,
            ref started, false);
        WriteDecimalDigit(destination, ref offset, ref remaining, 100000000u,
            ref started, false);
        WriteDecimalDigit(destination, ref offset, ref remaining, 10000000u,
            ref started, false);
        WriteDecimalDigit(destination, ref offset, ref remaining, 1000000u,
            ref started, false);
        WriteDecimalDigit(destination, ref offset, ref remaining, 100000u,
            ref started, false);
        WriteDecimalDigit(destination, ref offset, ref remaining, 10000u,
            ref started, false);
        WriteDecimalDigit(destination, ref offset, ref remaining, 1000u,
            ref started, false);
        WriteDecimalDigit(destination, ref offset, ref remaining, 100u,
            ref started, false);
        WriteDecimalDigit(destination, ref offset, ref remaining, 10u,
            ref started, false);
        WriteDecimalDigit(destination, ref offset, ref remaining, 1u,
            ref started, true);
    }

    private static void WriteDecimalDigit(APTR destination, ref int offset,
        ref uint remaining, uint place, ref bool started, bool alwaysWrite)
    {
        var digit = 0;
        while (remaining >= place)
        {
            remaining -= place;
            digit++;
        }
        if (digit != 0 || started || alwaysWrite)
        {
            APTR.WriteUInt8(destination, offset, (byte)(48 + digit));
            offset++;
            started = true;
        }
    }

    private static void ReadOutput(APTR header, BPTR file, APTR output)
    {
        APTR.WriteUInt32(header, Stage, 6);
        if (DOS.Flush(file) == 0)
        {
            Fail(header, FlushFailed, unchecked((int)DOS.IoErr()));
            return;
        }
        if (DOS.Seek(file, 0, 1) == -1) // OFFSET_END.
        {
            Fail(header, SeekFailed, unchecked((int)DOS.IoErr()));
            return;
        }
        // Seek returns the previous position. After seeking to EOF, this
        // second seek returns the full length and restores position zero.
        var length = DOS.Seek(file, 0, -1); // OFFSET_BEGINNING.
        if (length == -1)
        {
            Fail(header, SeekFailed, unchecked((int)DOS.IoErr()));
            return;
        }
        var wanted = length;
        if (wanted > OutputCapacity)
        {
            wanted = OutputCapacity;
            SetFlags(header, OutputTruncated);
            Fail(header, Truncated, 0);
        }
        var captured = 0;
        while (captured < wanted)
        {
            var read = DOS.Read(file, APTR.FromPointer(output.Raw + unchecked((uint)captured)), wanted - captured);
            if (read <= 0 || read > wanted - captured)
            {
                Fail(header, ReadFailed, unchecked((int)DOS.IoErr()));
                return;
            }
            captured += read;
            APTR.WriteUInt32(header, OutputLength, unchecked((uint)captured));
        }
        if (length <= OutputCapacity)
            SetFlags(header, OutputComplete);
    }

    private static void Fail(APTR header, uint failure, int ioError)
    {
        // Preserve the first probe failure while allowing later output capture
        // and cleanup to complete. Result/IoErr from System are independent.
        if (APTR.ReadUInt32(header, ProbeError) == 0)
        {
            APTR.WriteUInt32(header, ProbeError, failure);
            APTR.WriteUInt32(header, ErrorIoErr, unchecked((uint)ioError));
        }
    }

    private static void SetFlags(APTR header, uint flags) =>
        APTR.WriteUInt32(header, Flags, APTR.ReadUInt32(header, Flags) | flags);
}
