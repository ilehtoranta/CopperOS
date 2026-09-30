using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 <c>ExtractKickstart</c> body recovered from the V39.3
/// installer helper.  This is deliberately a raw trackdisk command: the
/// source inhibits the DOS device, reads the SuperKickstart boot block, and
/// copies the selected ROM image layout to a new DOS file.  The buffer,
/// message port, request, parser, and inhibition lease are invocation-local.
/// </summary>
public static class NativeWorkbench31ExtractKickstartCommand
{
    public const string Template = "DEVICE/A,TO/A,1.3/S";
    public const uint ResultCount = 3;
    public const uint BufferBytes = 0x40000;

    private const uint BootBlockBytes = 0x200;
    private const uint MetadataBytes = 0x400;
    private const uint LegacyChunkBytes = 0x40000;
    private const uint LegacyFirstOffset = 0x40400;
    private const uint LegacySecondOffset = 0x80400;
    private const uint LegacyThirdOffset = 0xc0400;
    private const uint Kickstart13Offset = 0xc1400;
    private const int ReadFailure = 10;
    private const int InvalidSuperKickstart = 5;
    private const int WriteFailure = 15;
    private const int InitialFailure = 20;
    private const int SuperKickstartError = 212;
    private const int BufferReadError = 219;

    public static int Run(out int ioError)
    {
        ioError = 0;
        APTR utility = APTR.Null;
        APTR buffer = APTR.Null;
        APTR messagePort = APTR.Null;
        APTR request = APTR.Null;
        BPTR output = BPTR.Null;
        var deviceInhibited = false;
        var deviceOpen = false;
        var result = InitialFailure;
        var error = 0;
        var diagnostic = 0;
        var arguments = default(NativeCommandArguments);

        do
        {
            // The replacement performs its bounded arithmetic locally and
            // calls no utility.library vector; retain the source lease at its
            // existence floor while the captured V36 request remains evidence.
            utility = Exec.OpenLibraryRaw(Utility.Name, 0);
            if (utility.IsNull)
                break;

            if (!NativeCommandArguments.TryRead(Template, ResultCount,
                    out arguments))
            {
                error = arguments.IoError;
                break;
            }

            if (!arguments.TryGetResult(0, out var device) ||
                !arguments.TryGetResult(1, out var destination) ||
                !arguments.TryGetResult(2, out var extract13) ||
                device == 0 || destination == 0)
            {
                error = (int)DOS.Error.RequiredArgumentMissing;
                break;
            }

            var devicePointer = APTR.FromPointer(device);
            if (!TryParseDevice(devicePointer, out var unit))
            {
                error = SuperKickstartError;
                break;
            }

            buffer = Exec.AllocVec(BufferBytes, (uint)Exec.MemoryFlags.Any);
            if (buffer.IsNull)
                break;

            messagePort = Exec.CreateMsgPort();
            if (messagePort.IsNull)
                break;
            request = Exec.CreateIORequest(messagePort, IOStdReq.Size);
            if (request.IsNull)
                break;

            // Preserve the source's raw-device exclusion window.  The source
            // ignores the Inhibit return value and always releases it during
            // cleanup after the device has been opened or attempted.
            _ = DOS.Inhibit(CString.FromPointer(devicePointer), -1);
            deviceInhibited = true;
            if (Exec.OpenDevice(TrackDiskDevice.Name, unit, request, 0) != 0)
            {
                error = 218;
                result = ReadFailure;
                diagnostic = 2;
                break;
            }
            deviceOpen = true;

            if (!ReadTrack(request, buffer, BootBlockBytes, 0))
            {
                result = ReadFailure;
                error = BufferReadError;
                diagnostic = 2;
                break;
            }

            if (APTR.ReadUInt32(buffer, 0) != 0x4b49434b ||
                APTR.ReadUInt32(buffer, 4) != 0x53555030)
            {
                result = InvalidSuperKickstart;
                error = SuperKickstartError;
                diagnostic = 3;
                break;
            }

            if (!ReadTrack(request, buffer, MetadataBytes, 0))
            {
                result = ReadFailure;
                error = BufferReadError;
                diagnostic = 2;
                break;
            }

            var kickstart13Bytes = APTR.ReadUInt32(buffer, 8);
            var legacyTailBytes = APTR.ReadUInt32(buffer, 12);
            output = DOS.OpenRaw(CString.FromPointer(destination),
                DOS.FileMode.NewFile);
            if (output.IsNull)
            {
                result = WriteFailure;
                error = (int)DOS.IoErr();
                if (error == 0) error = (int)DOS.Error.ObjectNotFound;
                diagnostic = 1;
                break;
            }

            if (extract13 != 0)
            {
                if (!CopyChunk(request, output, buffer, Kickstart13Offset,
                        kickstart13Bytes))
                {
                    result = WriteFailure;
                    error = (int)DOS.IoErr();
                    if (error == 0) error = BufferReadError;
                    diagnostic = 1;
                }
                else
                {
                    result = DOS.RETURN_OK;
                }
            }
            else if (!CopyChunk(request, output, buffer, LegacyFirstOffset,
                         LegacyChunkBytes) ||
                     !CopyChunk(request, output, buffer, LegacySecondOffset,
                         LegacyChunkBytes) ||
                     !CopyChunk(request, output, buffer, LegacyThirdOffset,
                         legacyTailBytes))
            {
                result = WriteFailure;
                error = (int)DOS.IoErr();
                if (error == 0) error = BufferReadError;
                diagnostic = 1;
            }
            else
            {
                result = DOS.RETURN_OK;
            }
        }
        while (false);

        // The original prints its contextual text while the ReadArgs result
        // pointers are still live, then releases the parser-owned storage.
        // Keep that ordering so a later FreeArgs cannot erase the diagnostic
        // arguments before PutStr observes them.
        if (diagnostic == 1 && arguments.IsSuccess &&
            arguments.TryGetResult(1, out var diagnosticDestination))
        {
            DOS.PutStr("Couldn't write ");
            DOS.PutStr(CString.FromPointer(diagnosticDestination));
            DOS.PutStr(" - ");
        }
        else if (diagnostic == 2 && arguments.IsSuccess &&
            arguments.TryGetResult(0, out var diagnosticDevice))
        {
            DOS.PutStr("Couldn't read from ");
            DOS.PutStr(CString.FromPointer(diagnosticDevice));
            DOS.PutStr(" - ");
        }
        else if (diagnostic == 3 && arguments.IsSuccess &&
            arguments.TryGetResult(0, out var invalidDevice))
        {
            DOS.PutStr(CString.FromPointer(invalidDevice));
            DOS.PutStr(" does not contain a SuperKickstart disk - ");
        }

        if (output.IsNotNull)
        {
            _ = DOS.Close(output);
            output = BPTR.Null;
            _ = DOS.SetProtection(CString.FromPointer(
                arguments.IsSuccess && arguments.TryGetResult(1,
                    out var destination) ? destination : 0), 2);
        }
        if (deviceInhibited)
        {
            if (arguments.IsSuccess && arguments.TryGetResult(0,
                    out var device))
                _ = DOS.Inhibit(CString.FromPointer(device), 0);
            deviceInhibited = false;
        }
        if (deviceOpen && request.IsNotNull)
            Exec.CloseDevice(request);
        if (request.IsNotNull)
            Exec.DeleteIORequest(request);
        if (messagePort.IsNotNull)
            Exec.DeleteMsgPort(messagePort);
        if (buffer.IsNotNull)
            Exec.FreeVec(buffer);
        if (arguments.IsSuccess)
            arguments.Release();
        if (utility.IsNotNull)
            Exec.CloseLibrary(utility);

        if (error != 0)
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
        ioError = error;
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static bool TryParseDevice(APTR device, out uint unit)
    {
        unit = 0;
        var first = APTR.ReadUInt8(device, 0);
        var second = APTR.ReadUInt8(device, 1);
        var digit = APTR.ReadUInt8(device, 2);
        if ((first != (byte)'d' && first != (byte)'D') ||
            (second != (byte)'f' && second != (byte)'F') ||
            digit < (byte)'0' || digit > (byte)'9' ||
            APTR.ReadUInt8(device, 3) != (byte)':')
            return false;
        unit = (uint)(digit - (byte)'0');
        return true;
    }

    private static bool ReadTrack(APTR request, APTR buffer, uint length,
        uint offset)
    {
        SetRequest(request, TrackDiskCommand.Read, buffer, length, offset);
        Exec.DoIO(request);
        var readError = APTR.ReadUInt8(request, ExecLayout.IOStdReq.Error);
        SetRequest(request, TrackDiskCommand.Motor, APTR.Null, 0, 0);
        Exec.DoIO(request);
        return readError == 0;
    }

    private static bool CopyChunk(APTR request, BPTR output, APTR buffer,
        uint offset, uint length)
    {
        if (length == 0 || length > BufferBytes ||
            !ReadTrack(request, buffer, length, offset))
            return false;
        return DOS.Write(output, buffer, unchecked((int)length)) > 0;
    }

    private static void SetRequest(APTR request, TrackDiskCommand command,
        APTR buffer, uint length, uint offset)
    {
        APTR.WriteUInt16(request, ExecLayout.IORequest.Command,
            (ushort)command);
        APTR.WriteUInt8(request, ExecLayout.IORequest.Flags, 0);
        APTR.WriteUInt32(request, ExecLayout.IOStdReq.Length, length);
        APTR.WriteUInt32(request, ExecLayout.IOStdReq.Data, buffer.Raw);
        APTR.WriteUInt32(request, ExecLayout.IOStdReq.Offset, offset);
    }

}
