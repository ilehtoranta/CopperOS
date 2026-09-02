using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded MorphOS Quote native body. It uses DOS ReadArgs and owns every
/// transient buffer per invocation; only the independently implemented STR
/// forward pipeline is admitted until FILE/VAR and presentation contracts are
/// resolved.
/// </summary>
public static class NativeMorphOSQuoteCommand
{
    private const uint InputBytes = 4096;
    private const uint RuleBytes = 64;
    private const uint OutputBytes = 4096;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(
                "RULE/A,FILE/K,VAR/K,STR,NOLINE/S,NOQUOTES/S,FIRSTLINE/S,REVERSE=UNQUOTE/S",
                8, out var arguments))
        {
            ioError = arguments.IoError;
            return arguments.ReturnLevel;
        }

        APTR input = APTR.Null;
        APTR ruleList = APTR.Null;
        APTR first = APTR.Null;
        APTR second = APTR.Null;
        APTR output = APTR.Null;
        var result = DOS.RETURN_ERROR;
        do
        {
            if (!arguments.TryGetResult(0, out var rules) ||
                !arguments.TryGetResult(1, out var file) ||
                !arguments.TryGetResult(2, out var variable) ||
                !arguments.TryGetResult(3, out var text) ||
                !arguments.TryGetResult(4, out var noLine) ||
                !arguments.TryGetResult(5, out var noQuotes) ||
                !arguments.TryGetResult(6, out var firstLine) ||
                !arguments.TryGetResult(7, out var reverse))
            {
                ioError = (int)DOS.Error.BadTemplate;
                break;
            }
            if (rules == 0 || file != 0 || variable != 0 || text == 0 ||
                noQuotes != 0 || firstLine != 0 || reverse != 0)
            {
                ioError = (int)DOS.Error.BadTemplate;
                break;
            }

            input = Exec.AllocMem(InputBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            ruleList = Exec.AllocMem(RuleBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            first = Exec.AllocMem(OutputBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            second = Exec.AllocMem(OutputBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            output = Exec.AllocMem(OutputBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (input.IsNull || ruleList.IsNull || first.IsNull || second.IsNull ||
                output.IsNull)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
                break;
            }
            if (!TryCopyCString(APTR.FromPointer(text), input, InputBytes,
                    out var inputLength) ||
                !TryCStringLength(APTR.FromPointer(rules), InputBytes,
                    out var rulesLength))
            {
                ioError = (int)DOS.Error.LineTooLong;
                break;
            }

            NativeGuestMemory memory = new(input);
            if (!QuoteRuleParser.TryParse(ref memory, APTR.FromPointer(rules),
                    rulesLength, ruleList, RuleBytes, out var ruleCount) ||
                !QuoteForwardPipeline.TryApply(ref memory, input, inputLength,
                    ruleList, ruleCount, first, OutputBytes, second, OutputBytes,
                    out var transformed, out var transformedLength) ||
                transformedLength > OutputBytes - (noLine == 0 ? 1u : 0u))
            {
                ioError = (int)DOS.Error.LineTooLong;
                break;
            }
            memory.Copy(transformed, output, transformedLength);
            var written = transformedLength;
            if (noLine == 0) memory.WriteUInt8(output, (int)written++, (byte)'\n');
            if (DOS.Write(DOS.Output(), output, unchecked((int)written)) !=
                unchecked((int)written))
            {
                ioError = (int)DOS.IoErr();
                break;
            }
            result = DOS.RETURN_OK;
        }
        while (false);

        if (output.IsNotNull) Exec.FreeMem(output, OutputBytes);
        if (second.IsNotNull) Exec.FreeMem(second, OutputBytes);
        if (first.IsNotNull) Exec.FreeMem(first, OutputBytes);
        if (ruleList.IsNotNull) Exec.FreeMem(ruleList, RuleBytes);
        if (input.IsNotNull) Exec.FreeMem(input, InputBytes);
        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static bool TryCopyCString(APTR source, APTR destination,
        uint capacity, out uint length)
    {
        length = 0;
        if (source.IsNull || destination.IsNull || capacity == 0) return false;
        for (; length < capacity; length++)
        {
            var value = APTR.ReadUInt8(source, (int)length);
            if (value == 0) return true;
            APTR.WriteUInt8(destination, (int)length, value);
        }
        return false;
    }

    private static bool TryCStringLength(APTR source, uint maximum,
        out uint length)
    {
        length = 0;
        if (source.IsNull) return false;
        for (; length < maximum; length++)
            if (APTR.ReadUInt8(source, (int)length) == 0) return true;
        return false;
    }

    private readonly struct NativeGuestMemory(APTR anchor) : IAmigaGuestMemory
    {
        public bool IsMapped(APTR address, uint byteSize) => anchor.IsNotNull &&
            address.IsNotNull && address.Raw <= uint.MaxValue - byteSize;
        public byte ReadUInt8(APTR address, int offset = 0) => APTR.ReadUInt8(address, offset);
        public ushort ReadUInt16(APTR address, int offset = 0) => APTR.ReadUInt16(address, offset);
        public uint ReadUInt32(APTR address, int offset = 0) => APTR.ReadUInt32(address, offset);
        public void WriteUInt8(APTR address, int offset, byte value) => APTR.WriteUInt8(address, offset, value);
        public void WriteUInt16(APTR address, int offset, ushort value) => APTR.WriteUInt16(address, offset, value);
        public void WriteUInt32(APTR address, int offset, uint value) => APTR.WriteUInt32(address, offset, value);
        public void Clear(APTR address, uint byteCount)
        {
            for (var index = 0u; index < byteCount; index++)
                APTR.WriteUInt8(address, (int)index, 0);
        }
        public void Copy(APTR source, APTR destination, uint byteCount)
        {
            for (var index = 0u; index < byteCount; index++)
                APTR.WriteUInt8(destination, (int)index,
                    APTR.ReadUInt8(source, (int)index));
        }
    }
}
