using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Source-observed MorphOS AddBuffers command body.</summary>
public static class NativeMorphOSAddBuffersCommand
{
    private const uint FormatBytes = NativeAddBuffersOutputFormatRecordCodec.Size;

    /// <summary>
    /// Uses the public DOS AddBuffers vector after DOS ReadArgs. The caller owns
    /// startup and final library teardown; this body owns parser and format
    /// storage for this invocation only.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(
                NativeAddBuffersReadArgsRecordCodec.Template,
                NativeAddBuffersReadArgsRecordCodec.ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            return arguments.ReturnLevel;
        }

        var format = APTR.Null;
        var result = DOS.RETURN_OK;
        do
        {
            if (!NativeAddBuffersReadArgsRecordCodec.TryRead(ref arguments,
                    out var parsed))
            {
                ioError = (int)DOS.Error.BadTemplate;
                result = DOS.RETURN_FAIL;
                break;
            }
            var changed = DOS.AddBuffers(CString.FromPointer(parsed.Drive.Raw),
                parsed.Buffers);
            if (changed == 0)
            {
                ioError = (int)DOS.IoErr();
                DOS.PrintFault((DOS.Error)ioError, "AddBuffers");
                result = DOS.RETURN_FAIL;
                break;
            }
            format = Exec.AllocMem(FormatBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (format.IsNull)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
                break;
            }
            var output = default(NativeAddBuffersOutputFormatRecord);
            output.Drive = parsed.Drive;
            output.Buffers = changed == -1 ? (int)DOS.IoErr() : changed;
            if (!NativeAddBuffersOutputFormatRecordCodec.TryWrite(format,
                    FormatBytes, output))
            {
                ioError = (int)DOS.Error.BadTemplate;
                result = DOS.RETURN_FAIL;
                break;
            }
            DOS.VPrintf("%s has %ld buffers\n", format);
        }
        while (false);
        if (format.IsNotNull) Exec.FreeMem(format, FormatBytes);
        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }
}
