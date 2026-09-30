using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 RequestChoice body based on the captured v40.1 four-slot
/// boundary.  The requester remains owned by Intuition; parser and temporary
/// text storage are released before the entry closes the library lease.
/// </summary>
public static class NativeWorkbench31RequestChoiceCommand
{
    public const string Template =
        "TITLE/A,BODY/A,GADGETS/M,PUBSCREEN/K";
    public const uint ResultCount = 4;

    private struct PrintFields
    {
        public int Result;

        public static APTR AddressOf(ref PrintFields fields) =>
            throw new System.NotSupportedException(
                "Workbench31RequestChoice.PrintFields.AddressOf is lowered by CopperSharp.");
    }

    private struct EasyFields
    {
        public EasyStruct Value;

        public static APTR AddressOf(ref EasyFields fields) =>
            throw new System.NotSupportedException(
                "Workbench31RequestChoice.EasyFields.AddressOf is lowered by CopperSharp.");
    }

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "RequestChoice");
            return arguments.ReturnLevel;
        }

        APTR gadgetText = APTR.Null;
        APTR filteredBody = APTR.Null;
        var result = DOS.RETURN_OK;
        var error = 0;
        do
        {
            if (!arguments.TryGetResult(0, out var title) ||
                !arguments.TryGetResult(1, out var body) ||
                !arguments.TryGetResult(2, out var gadgets) ||
                !arguments.TryGetResult(3, out var publicScreen))
            {
                error = (int)DOS.Error.BadTemplate;
                result = DOS.RETURN_FAIL;
                break;
            }

            gadgetText = ComposeGadgetText(APTR.FromPointer(gadgets));
            if (gadgetText.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
                break;
            }

            var bodyPointer = APTR.FromPointer(body);
            if (bodyPointer.IsNotNull && ContainsPercent(bodyPointer))
            {
                if (!TryFilterBody(bodyPointer, out filteredBody))
                {
                    error = (int)DOS.Error.NoFreeStore;
                    result = DOS.RETURN_FAIL;
                    break;
                }
                bodyPointer = filteredBody;
            }

            var screen = APTR.FromPointer(Intuition.LockPubScreen(publicScreen));
            if (screen.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
                break;
            }

            var easy = default(EasyFields);
            easy.Value.StructureSize = EasyStruct.Size;
            easy.Value.Flags = 0;
            easy.Value.Title = STRPTR.FromPointer(title);
            easy.Value.TextFormat = STRPTR.FromAddress(bodyPointer);
            easy.Value.GadgetFormat = STRPTR.FromAddress(gadgetText);
            var easyAddress = EasyFields.AddressOf(ref easy);
            var choice = Intuition.EasyRequestArgs(
                APTR.FromPointer(APTR.ReadUInt32(screen, 4)).Raw,
                easyAddress.Raw, 0, 0);
            Intuition.UnlockPubScreen(0, screen.Raw);
            if (choice < 0)
            {
                error = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
                break;
            }
            PrintChoice(choice);
        }
        while (false);

        if (gadgetText.IsNotNull) Exec.FreeVec(gadgetText);
        if (filteredBody.IsNotNull) Exec.FreeVec(filteredBody);
        arguments.Release();

        if (error != 0)
        {
            ioError = error;
            DOS.SetIoErr((DOS.Error)error);
            DOS.PrintFault((DOS.Error)error, "RequestChoice");
        }
        else
        {
            DOS.SetIoErr((DOS.Error)0);
        }
        return result;
    }

    private static APTR ComposeGadgetText(APTR gadgets)
    {
        if (gadgets.IsNull) return APTR.Null;
        var length = 1u;
        var count = 0u;
        while (count < 256)
        {
            var gadget = APTR.FromPointer(APTR.ReadUInt32(gadgets,
                unchecked((int)(count * 4))));
            if (gadget.IsNull) break;
            var textLength = CStringLength(gadget);
            if (uint.MaxValue - length < textLength + 1) return APTR.Null;
            length += textLength + 1;
            count++;
        }
        if (count == 256 && APTR.ReadUInt32(gadgets, 256 * 4) != 0)
            return APTR.Null;

        var result = Exec.AllocVec(length, (uint)(Exec.MemoryFlags.Public |
            Exec.MemoryFlags.Clear));
        if (result.IsNull) return APTR.Null;
        var output = 0u;
        for (var index = 0u; index < count; index++)
        {
            var gadget = APTR.FromPointer(APTR.ReadUInt32(gadgets,
                unchecked((int)(index * 4))));
            var textLength = CStringLength(gadget);
            CopyBytes(gadget, APTR.FromPointer(result.Raw + output), textLength);
            output += textLength;
            if (index + 1 < count)
                APTR.WriteUInt8(APTR.FromPointer(result.Raw + output++), 0,
                    (byte)'|');
        }
        APTR.WriteUInt8(APTR.FromPointer(result.Raw + output), 0, 0);
        return result;
    }

    private static bool ContainsPercent(APTR source)
    {
        for (var index = 0u;; index++)
        {
            var value = APTR.ReadUInt8(source, unchecked((int)index));
            if (value == 0) return false;
            if (value == (byte)'%') return true;
        }
    }

    private static bool TryFilterBody(APTR source, out APTR result)
    {
        result = APTR.Null;
        var length = CStringLength(source);
        var extra = 0u;
        for (var index = 0u; index < length; index++)
            if (APTR.ReadUInt8(source, unchecked((int)index)) == (byte)'%')
                extra++;
        if (uint.MaxValue - length < extra + 1) return false;
        result = Exec.AllocVec(length + extra + 1, (uint)(
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
        if (result.IsNull) return false;
        var output = 0u;
        for (var index = 0u; index < length; index++)
        {
            var value = APTR.ReadUInt8(source, unchecked((int)index));
            APTR.WriteUInt8(APTR.FromPointer(result.Raw + output++), 0, value);
            if (value == (byte)'%')
                APTR.WriteUInt8(APTR.FromPointer(result.Raw + output++), 0,
                    (byte)'%');
        }
        APTR.WriteUInt8(APTR.FromPointer(result.Raw + output), 0, 0);
        return true;
    }

    private static void PrintChoice(int result)
    {
        var fields = new PrintFields { Result = result };
        DOS.VPrintf("%ld\n", PrintFields.AddressOf(ref fields));
    }

    private static uint CStringLength(APTR source)
    {
        for (var index = 0u;; index++)
            if (APTR.ReadUInt8(source, unchecked((int)index)) == 0)
                return index;
    }

    private static void CopyBytes(APTR source, APTR destination, uint length)
    {
        for (var index = 0u; index < length; index++)
            APTR.WriteUInt8(destination, unchecked((int)index),
                APTR.ReadUInt8(source, unchecked((int)index)));
    }
}
