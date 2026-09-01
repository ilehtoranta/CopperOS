using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.NativeRoot;

/// <summary>
/// Private CC04 argument-lease fixture, never a shipping command or parser.
/// The launch block is four big-endian ULONGs: CC04, case, expected numeric
/// value, and expected ambient IoErr after a supplied successful parse.
/// DOS still receives its ordinary ReadArgs input/source convention.
/// </summary>
public static class NativeArgumentBoundaryProbe
{
    private const uint ProtocolMagic = 0x43433034;
    private const int ProtocolBytes = 16;
    private const int FirstReleaseError = (int)DOS.Error.ObjectNotFound;
    private const int RepeatedReleaseError = (int)DOS.Error.ObjectInUse;

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(36))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);

        if (workbench.IsNotNull || argumentLength != ProtocolBytes ||
            argumentText.IsNull || APTR.ReadUInt32(argumentText.Address, 0) != ProtocolMagic)
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL,
                (int)DOS.Error.BadTemplate, workbench);

        var scenario = APTR.ReadUInt32(argumentText.Address, 4);
        var expectedNumber = APTR.ReadUInt32(argumentText.Address, 8);
        var expectedAmbientError = (int)APTR.ReadUInt32(argumentText.Address, 12);
        if (scenario == 7)
        {
            NativeCommandArguments empty = default;
            empty.Release();
            empty.Release();
            DOS.SetIoErr((DOS.Error)RepeatedReleaseError);
            empty.Release();
            if (empty.IsSuccess || empty.ResultCount != 0 ||
                empty.ReturnLevel != 0 || empty.IoError != 0 ||
                empty.TryGetResult(0, out var emptyValue) || emptyValue != 0 ||
                empty.TryGetResult(uint.MaxValue, out emptyValue) || emptyValue != 0 ||
                (int)DOS.IoErr() != RepeatedReleaseError)
                return InvalidState(ref empty, workbench);
            return NativeCommandStartup.Finish(DOS.RETURN_OK,
                RepeatedReleaseError, workbench);
        }

        CString template = "VALUE/N";
        uint resultCount = 1;
        if (scenario == 0)
        {
            template = "";
            resultCount = 0;
        }
        else if (scenario == 1 || scenario == 8)
        {
            template = CString.FromPointer(0);
            resultCount = scenario == 8 ? 0u : 1u;
        }
        else if (scenario == 2)
            resultCount = 0x40000000; // ULONG multiplication would wrap.
        else if (scenario == 3)
            resultCount = 0x3fffffff; // Byte count fits, Exec rounding would wrap.
        else if (scenario == 4)
            resultCount = 0x3ffffffe; // Maximum safe count; fixture fails AllocMem.
        else if (scenario != 5 && scenario != 6)
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL,
                (int)DOS.Error.BadTemplate, workbench);

        if (!NativeCommandArguments.TryRead(template, resultCount, out var arguments))
        {
            var result = arguments.ReturnLevel;
            var error = arguments.IoError;
            if (arguments.IsSuccess || arguments.ResultCount != 0 ||
                arguments.TryGetResult(0, out var failedValue) || failedValue != 0 ||
                arguments.TryGetResult(uint.MaxValue, out failedValue) || failedValue != 0 ||
                (result != DOS.RETURN_ERROR && result != DOS.RETURN_FAIL) ||
                (int)DOS.IoErr() != error)
                return InvalidState(ref arguments, workbench);

            DOS.SetIoErr((DOS.Error)RepeatedReleaseError);
            arguments.Release();
            arguments.Release();
            if (arguments.ReturnLevel != result || arguments.IoError != error ||
                (int)DOS.IoErr() != RepeatedReleaseError)
                return InvalidState(ref arguments, workbench);
            return NativeCommandStartup.Finish(result, error, workbench);
        }

        if (!arguments.IsSuccess || arguments.ResultCount != resultCount ||
            arguments.ReturnLevel != DOS.RETURN_OK || arguments.IoError != 0 ||
            (int)DOS.IoErr() != expectedAmbientError)
            return InvalidState(ref arguments, workbench);

        if (resultCount == 0)
        {
            if (arguments.TryGetResult(0, out var emptyValue) || emptyValue != 0 ||
                arguments.TryGetResult(uint.MaxValue, out emptyValue) || emptyValue != 0)
                return InvalidState(ref arguments, workbench);
        }
        else
        {
            if (!arguments.TryGetResult(0, out var numberPointer) || numberPointer == 0 ||
                APTR.ReadUInt32(APTR.FromPointer(numberPointer), 0) != expectedNumber ||
                arguments.TryGetResult(1, out var invalidValue) || invalidValue != 0 ||
                arguments.TryGetResult(uint.MaxValue, out invalidValue) || invalidValue != 0)
                return InvalidState(ref arguments, workbench);
        }

        DOS.SetIoErr((DOS.Error)FirstReleaseError);
        arguments.Release();
        if (arguments.IsSuccess || arguments.ResultCount != 0 ||
            arguments.ReturnLevel != DOS.RETURN_OK || arguments.IoError != 0 ||
            arguments.TryGetResult(0, out var releasedValue) || releasedValue != 0 ||
            arguments.TryGetResult(uint.MaxValue, out releasedValue) || releasedValue != 0 ||
            (int)DOS.IoErr() != FirstReleaseError)
            return InvalidState(ref arguments, workbench);

        DOS.SetIoErr((DOS.Error)RepeatedReleaseError);
        arguments.Release();
        arguments.Release();
        if ((int)DOS.IoErr() != RepeatedReleaseError)
            return InvalidState(ref arguments, workbench);
        return NativeCommandStartup.Finish(DOS.RETURN_OK,
            RepeatedReleaseError, workbench);
    }

    private static int InvalidState(ref NativeCommandArguments arguments, APTR workbench)
    {
        arguments.Release();
        return NativeCommandStartup.Finish(DOS.RETURN_FAIL,
            (int)DOS.Error.BadTemplate, workbench);
    }
}
