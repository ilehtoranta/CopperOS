using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Versioned DOS outer grammars for the native Eval command body.</summary>
public enum NativeEvalProfile : byte
{
    Workbench31,
    MorphOS320,
}

/// <summary>
/// Native Eval body using actual DOS ReadArgs and invocation-owned memory. The
/// caller opens dos.library and publishes the final result/error.
/// </summary>
public static class NativeEvalCommand
{
    private const uint ResultSlots = 6;
    private const uint ExpressionBytes = 4096;
    private const uint OutputBytes = 4096;
    private const uint MaximumValues = 64;

    /// <summary>
    /// Runs the captured Workbench 3.1 subset with literal classic template
    /// and formatting/evaluator paths. This separate entry avoids relying on
    /// an enum comparison in resident code to select a public DOS grammar.
    /// </summary>
    public static int RunWorkbench31(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead("VALUE1/A,OP,VALUE2/M,TO/K,LFORMAT/K",
                5, out var arguments))
        {
            ioError = arguments.IoError;
            return arguments.ReturnLevel;
        }

        APTR expression = APTR.Null;
        APTR output = APTR.Null;
        BPTR destination = BPTR.Null;
        var closeDestination = false;
        var result = DOS.RETURN_ERROR;
        do
        {
            if (!arguments.TryGetResult(0, out var first) ||
                !arguments.TryGetResult(1, out var op) ||
                !arguments.TryGetResult(2, out var following) ||
                !arguments.TryGetResult(3, out var to) ||
                !arguments.TryGetResult(4, out var format))
            {
                ioError = (int)DOS.Error.BadTemplate;
                break;
            }

            expression = Exec.AllocMem(ExpressionBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            output = Exec.AllocMem(OutputBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (expression.IsNull || output.IsNull)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
                break;
            }

            uint expressionLength = 0;
            if (first == 0 || !TryAppendCString(APTR.FromPointer(first), expression,
                    ExpressionBytes, ref expressionLength) ||
                (op != 0 && !TryAppendCString(APTR.FromPointer(op), expression,
                    ExpressionBytes, ref expressionLength)) ||
                !TryAppendMultiple(APTR.FromPointer(following), expression,
                    ExpressionBytes, ref expressionLength))
            {
                ioError = (int)DOS.Error.LineTooLong;
                break;
            }

            NativeGuestMemory memory = new(expression);
            if (!EvalExpressionEvaluator.TryEvaluateWorkbench31Arithmetic(ref memory,
                    expression, expressionLength, out var value, out _)) break;

            uint written;
            if (format != 0)
            {
                if (!TryCStringLength(APTR.FromPointer(format), ExpressionBytes,
                        out var formatLength) || !EvalClassicLFormatFormatter.TryFormat(
                        ref memory, APTR.FromPointer(format), formatLength, value,
                        output, OutputBytes, out written, out _)) break;
            }
            else if (!EvalNumericFormatter.TryWriteDecimal(ref memory, value, output,
                OutputBytes, true, out written)) break;

            destination = DOS.Output();
            if (to != 0)
            {
                var opened = DOS.Open(CString.FromPointer(to), DOS.FileMode.NewFile);
                if (!opened.HasValue)
                {
                    ioError = (int)DOS.IoErr();
                    result = DOS.RETURN_FAIL;
                    break;
                }
                destination = opened.Value;
                closeDestination = true;
            }
            if (DOS.Write(destination, output, unchecked((int)written)) !=
                unchecked((int)written))
            {
                ioError = (int)DOS.IoErr();
                break;
            }
            result = DOS.RETURN_OK;
            break;
        }
        while (false);

        if (closeDestination) DOS.Close(destination);
        if (output.IsNotNull) Exec.FreeMem(output, OutputBytes);
        if (expression.IsNotNull) Exec.FreeMem(expression, ExpressionBytes);
        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    public static int Run(NativeEvalProfile profile, out int ioError)
    {
        ioError = 0;
        CString template = profile == NativeEvalProfile.MorphOS320
            ? "VALUE1/A,OP,VALUE2/M,TO/K,LFORMAT/K,HEX/S"
            : "VALUE1/A,OP,VALUE2/M,TO/K,LFORMAT/K";
        uint resultCount = profile == NativeEvalProfile.MorphOS320 ? ResultSlots : 5;
        if (!NativeCommandArguments.TryRead(template, resultCount, out var arguments))
        {
            ioError = arguments.IoError;
            return arguments.ReturnLevel;
        }

        APTR expression = APTR.Null;
        APTR output = APTR.Null;
        BPTR destination = BPTR.Null;
        uint hex = 0;
        var closeDestination = false;
        var result = DOS.RETURN_ERROR;
        do
        {
            if (!arguments.TryGetResult(0, out var first) ||
                !arguments.TryGetResult(1, out var op) ||
                !arguments.TryGetResult(2, out var following) ||
                !arguments.TryGetResult(3, out var to) ||
                !arguments.TryGetResult(4, out var format) ||
                (profile == NativeEvalProfile.MorphOS320 &&
                 !arguments.TryGetResult(5, out hex)))
            {
                ioError = (int)DOS.Error.BadTemplate;
                break;
            }

            expression = Exec.AllocMem(ExpressionBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            output = Exec.AllocMem(OutputBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (expression.IsNull || output.IsNull)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
                break;
            }

            uint expressionLength = 0;
            if (first == 0 || !TryAppendCString(APTR.FromPointer(first), expression,
                    ExpressionBytes, ref expressionLength) ||
                (op != 0 && !TryAppendCString(APTR.FromPointer(op), expression,
                    ExpressionBytes, ref expressionLength)) ||
                !TryAppendMultiple(APTR.FromPointer(following), expression,
                    ExpressionBytes, ref expressionLength))
            {
                ioError = (int)DOS.Error.LineTooLong;
                break;
            }

            NativeGuestMemory memory = new(expression);
            var evaluated = profile == NativeEvalProfile.Workbench31
                ? EvalExpressionEvaluator.TryEvaluateWorkbench31Arithmetic(
                    ref memory, expression, expressionLength, out var value, out _)
                : EvalExpressionEvaluator.TryEvaluate(ref memory, expression,
                    expressionLength, out value, out _);
            if (!evaluated)
                break;

            uint written;
            if (format != 0)
            {
                if (!TryCStringLength(APTR.FromPointer(format), ExpressionBytes,
                        out var formatLength) || !(profile == NativeEvalProfile.Workbench31
                        ? EvalClassicLFormatFormatter.TryFormat(ref memory,
                            APTR.FromPointer(format), formatLength, value, output,
                            OutputBytes, out written, out _)
                        : EvalLFormatFormatter.TryFormat(ref memory,
                            APTR.FromPointer(format), formatLength, value, output,
                            OutputBytes, out written, out _)))
                    break;
            }
            else if (profile == NativeEvalProfile.MorphOS320 && hex != 0)
            {
                if (!EvalNumericFormatter.TryWriteHexadecimal(ref memory,
                        unchecked((ulong)value), output, OutputBytes, true, true,
                        out written)) break;
            }
            else if (!EvalNumericFormatter.TryWriteDecimal(ref memory, value, output,
                OutputBytes, true, out written)) break;

            destination = DOS.Output();
            if (to != 0)
            {
                var opened = DOS.Open(CString.FromPointer(to), DOS.FileMode.NewFile);
                if (!opened.HasValue)
                {
                    ioError = (int)DOS.IoErr();
                    result = DOS.RETURN_FAIL;
                    break;
                }
                destination = opened.Value;
                closeDestination = true;
            }
            if (DOS.Write(destination, output, unchecked((int)written)) !=
                unchecked((int)written))
            {
                ioError = (int)DOS.IoErr();
                break;
            }
            result = DOS.RETURN_OK;
            break;
        }
        while (false);

        if (closeDestination) DOS.Close(destination);
        if (output.IsNotNull) Exec.FreeMem(output, OutputBytes);
        if (expression.IsNotNull) Exec.FreeMem(expression, ExpressionBytes);
        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static bool TryAppendMultiple(APTR values, APTR destination,
        uint capacity, ref uint count)
    {
        if (values.IsNull) return true;
        for (var index = 0u; index < MaximumValues; index++)
        {
            var value = APTR.ReadUInt32(values, (int)(index * sizeof(uint)));
            if (value == 0) return true;
            if (!TryAppendCString(APTR.FromPointer(value), destination, capacity,
                    ref count)) return false;
        }
        return false;
    }

    private static bool TryAppendCString(APTR source, APTR destination,
        uint capacity, ref uint count)
    {
        if (source.IsNull || count >= capacity) return false;
        if (count != 0)
        {
            if (count + 1 >= capacity) return false;
            APTR.WriteUInt8(destination, (int)count++, (byte)' ');
        }
        for (var index = 0u; index < ExpressionBytes; index++)
        {
            if (count >= capacity) return false;
            var value = APTR.ReadUInt8(source, (int)index);
            if (value == 0) return true;
            APTR.WriteUInt8(destination, (int)count++, value);
        }
        return false;
    }

    private static bool TryCStringLength(APTR source, uint maximum, out uint length)
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
            address.IsNotNull &&
            address.Raw <= uint.MaxValue - byteSize;
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
