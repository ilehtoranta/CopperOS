using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench AddBuffers 37.2 behavior observed by executing the original
/// against supplied public DOS vectors. Startup and library ownership belong
/// to the caller; all parser/format storage belongs to this invocation.
/// </summary>
public static class NativeWorkbench31AddBuffersCommand
{
    public const string Template = NativeAddBuffersReadArgsRecordCodec.Template;
    public const uint ResultCount = NativeAddBuffersReadArgsRecordCodec.ResultCount;
    private const uint ResultArrayBytes =
        NativeAddBuffersReadArgsRecordCodec.ResultArrayBytes;

    public static int Run(out int ioError)
    {
        ioError = 0;
        var slots = Exec.AllocMem(ResultArrayBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (slots.IsNull)
        {
            ioError = (int)DOS.Error.NoFreeStore;
            DOS.PrintFault(DOS.Error.NoFreeStore, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var parsed = DOS.ReadArgs(Template, slots, APTR.Null);
        var result = DOS.RETURN_FAIL;
        if (parsed.IsNull)
            DOS.PrintFault(DOS.IoErr(), CString.FromPointer(0));
        else
        {
            result = DOS.RETURN_OK;
            if (!NativeAddBuffersReadArgsRecordCodec.TryRead(slots,
                    ResultArrayBytes, out var arguments))
            {
                DOS.SetIoErr(DOS.Error.BadTemplate);
                DOS.PrintFault(DOS.Error.BadTemplate, CString.FromPointer(0));
                result = DOS.RETURN_FAIL;
            }
            else
            {
                // Even an explicit numeric zero takes the change-then-query
                // path. Classic behavior does not reinterpret -1 as IoErr.
                var changed = !arguments.HasBuffers ? 1 : DOS.AddBuffers(
                    CString.FromPointer(arguments.Drive.Raw), arguments.Buffers);
                if (changed == 0)
                    DOS.PrintFault(DOS.IoErr(), CString.FromPointer(0));
                else
                {
                    var queried = DOS.AddBuffers(
                        CString.FromPointer(arguments.Drive.Raw), 0);
                    if (queried == 0)
                        DOS.PrintFault(DOS.IoErr(), CString.FromPointer(0));
                    else if (queried > 0)
                    {
                        var output = default(NativeAddBuffersOutputFormatRecord);
                        output.Drive = arguments.Drive;
                        output.Buffers = queried;
                        if (NativeAddBuffersOutputFormatRecordCodec.TryWrite(
                                slots, ResultArrayBytes, output))
                            DOS.VPrintf("%s has %ld buffers\n", slots);
                    }
                }
            }
            DOS.FreeArgs(parsed);
        }

        // The original leaves FreeArgs' ambient error intact. Capture it
        // before freeing our additional result storage and closing DOS.
        ioError = (int)DOS.IoErr();
        Exec.FreeMem(slots, ResultArrayBytes);
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }
}
