using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands;

/// <summary>Selectable outer grammars for the bounded Eval command core.</summary>
public enum EvalCommandProfile : byte
{
    Workbench31,
    MorphOS320,
}

/// <summary>
/// Bounded external Eval command core. Argument storage and all working buffers
/// are supplied by the caller so evaluation retains no process or resident state.
/// </summary>
public static class EvalCommand
{
    private const uint ResultSlots = 6;
    private const uint MaximumValues = 64;

    public static int Execute<TPlatform>(ref TPlatform platform,
        in CommandInvocation invocation, EvalCommandProfile profile,
        APTR template, uint templateCapacity, APTR results, uint resultsCapacity,
        APTR expression, uint expressionCapacity, APTR output, uint outputCapacity)
        where TPlatform : struct, IShellPlatform
    {
        uint requiredTemplateCapacity = profile == EvalCommandProfile.MorphOS320
            ? 42u : 36u;
        if (template.IsNull || results.IsNull || expression.IsNull || output.IsNull ||
            templateCapacity < requiredTemplateCapacity || resultsCapacity < ResultSlots * 4 ||
            expressionCapacity == 0 || outputCapacity == 0 ||
            !platform.IsMapped(template, templateCapacity) ||
            !platform.IsMapped(results, ResultSlots * 4) ||
            !platform.IsMapped(expression, expressionCapacity) ||
            !platform.IsMapped(output, outputCapacity))
            return (int)ShellCommandResult.Fail;

        for (var slot = 0; slot < ResultSlots; slot++)
            platform.WriteUInt32(results, (int)(slot * 4), 0);
        var templateLength = WriteTemplate(ref platform, template, profile);
        if (!platform.TryReadArgs(invocation.ArgumentText, invocation.ArgumentLength,
                template, templateLength, results, ResultSlots * 4, out var rdArgs))
            return (int)ShellCommandResult.Fail;

        try
        {
            var expressionLength = 0u;
            var first = APTR.FromPointer(platform.ReadUInt32(results, 0));
            var op = APTR.FromPointer(platform.ReadUInt32(results, 4));
            var following = APTR.FromPointer(platform.ReadUInt32(results, 8));
            var to = APTR.FromPointer(platform.ReadUInt32(results, 12));
            var format = APTR.FromPointer(platform.ReadUInt32(results, 16));
            var hex = profile == EvalCommandProfile.MorphOS320
                ? platform.ReadUInt32(results, 20) != 0 : false;
            if (first.IsNull || !TryAppendCString(ref platform, first, expression,
                    expressionCapacity, ref expressionLength) ||
                !TryAppendOptional(ref platform, op, expression, expressionCapacity,
                    ref expressionLength) ||
                !TryAppendMultiple(ref platform, following, expression,
                    expressionCapacity, ref expressionLength))
                return (int)ShellCommandResult.Error;

            var evaluated = profile == EvalCommandProfile.Workbench31
                ? EvalExpressionEvaluator.TryEvaluateWorkbench31Arithmetic(
                    ref platform, expression, expressionLength, out var value, out _)
                : EvalExpressionEvaluator.TryEvaluate(ref platform, expression,
                    expressionLength, out value, out _);
            if (!evaluated)
                return (int)ShellCommandResult.Error;

            uint written;
            if (format.IsNotNull)
            {
                if (!CStringCodec.TryReadLength(ref platform, format, 4096,
                        out var formatLength) ||
                    !(profile == EvalCommandProfile.Workbench31
                        ? EvalClassicLFormatFormatter.TryFormat(ref platform, format,
                            formatLength, value, output, outputCapacity, out written, out _)
                        : EvalLFormatFormatter.TryFormat(ref platform, format,
                            formatLength, value, output, outputCapacity, out written, out _)))
                    return (int)ShellCommandResult.Error;
            }
            else if (hex)
            {
                if (!EvalNumericFormatter.TryWriteHexadecimal(ref platform,
                        unchecked((ulong)value), output, outputCapacity, true, true,
                        out written)) return (int)ShellCommandResult.Error;
            }
            else if (!EvalNumericFormatter.TryWriteDecimal(ref platform, value,
                output, outputCapacity, true, out written)) return (int)ShellCommandResult.Error;

            var destination = invocation.Output;
            var closeDestination = false;
            if (to.IsNotNull)
            {
                if (!CStringCodec.TryReadLength(ref platform, to, 4096,
                        out var toLength)) return (int)ShellCommandResult.Error;
                destination = platform.OpenOutput(to, toLength);
                if (destination.IsNull) return (int)ShellCommandResult.Fail;
                closeDestination = true;
            }
            var result = platform.Write(destination, output, written) == (int)written
                ? (int)ShellCommandResult.Ok : (int)ShellCommandResult.Error;
            if (closeDestination) platform.CloseOutput(destination);
            return result;
        }
        finally { platform.FreeArgs(rdArgs); }
    }

    private static bool TryAppendOptional<TPlatform>(ref TPlatform platform,
        APTR value, APTR destination, uint capacity, ref uint count)
        where TPlatform : struct, IShellPlatform => value.IsNull ||
        TryAppendCString(ref platform, value, destination, capacity, ref count);

    private static bool TryAppendMultiple<TPlatform>(ref TPlatform platform,
        APTR values, APTR destination, uint capacity, ref uint count)
        where TPlatform : struct, IShellPlatform
    {
        if (values.IsNull) return true;
        for (var index = 0u; index < MaximumValues; index++)
        {
            if (!platform.IsMapped(values, (index + 1) * 4)) return false;
            var value = APTR.FromPointer(platform.ReadUInt32(values, (int)(index * 4)));
            if (value.IsNull) return true;
            if (!TryAppendCString(ref platform, value, destination, capacity, ref count))
                return false;
        }
        return false;
    }

    private static bool TryAppendCString<TPlatform>(ref TPlatform platform,
        APTR value, APTR destination, uint capacity, ref uint count)
        where TPlatform : struct, IShellPlatform
    {
        if (!CStringCodec.TryReadLength(ref platform, value, 4096, out var length) ||
            length > capacity - count || (count != 0 && length >= capacity - count)) return false;
        if (count != 0) platform.WriteUInt8(destination, (int)count++, (byte)' ');
        platform.Copy(value, APTR.FromPointer(destination.Raw + count), length);
        count += length;
        return true;
    }

    private static uint WriteTemplate<TPlatform>(ref TPlatform platform, APTR buffer,
        EvalCommandProfile profile) where TPlatform : struct, IShellPlatform
    {
        WriteByte(ref platform, buffer, 0, (byte)'V');
        WriteByte(ref platform, buffer, 1, (byte)'A');
        WriteByte(ref platform, buffer, 2, (byte)'L');
        WriteByte(ref platform, buffer, 3, (byte)'U');
        WriteByte(ref platform, buffer, 4, (byte)'E');
        WriteByte(ref platform, buffer, 5, (byte)'1');
        WriteByte(ref platform, buffer, 6, (byte)'/');
        WriteByte(ref platform, buffer, 7, (byte)'A');
        WriteByte(ref platform, buffer, 8, (byte)',');
        WriteByte(ref platform, buffer, 9, (byte)'O');
        WriteByte(ref platform, buffer, 10, (byte)'P');
        WriteByte(ref platform, buffer, 11, (byte)',');
        WriteByte(ref platform, buffer, 12, (byte)'V');
        WriteByte(ref platform, buffer, 13, (byte)'A');
        WriteByte(ref platform, buffer, 14, (byte)'L');
        WriteByte(ref platform, buffer, 15, (byte)'U');
        WriteByte(ref platform, buffer, 16, (byte)'E');
        WriteByte(ref platform, buffer, 17, (byte)'2');
        WriteByte(ref platform, buffer, 18, (byte)'/');
        WriteByte(ref platform, buffer, 19, (byte)'M');
        WriteByte(ref platform, buffer, 20, (byte)',');
        WriteByte(ref platform, buffer, 21, (byte)'T');
        WriteByte(ref platform, buffer, 22, (byte)'O');
        WriteByte(ref platform, buffer, 23, (byte)'/');
        WriteByte(ref platform, buffer, 24, (byte)'K');
        WriteByte(ref platform, buffer, 25, (byte)',');
        WriteByte(ref platform, buffer, 26, (byte)'L');
        WriteByte(ref platform, buffer, 27, (byte)'F');
        WriteByte(ref platform, buffer, 28, (byte)'O');
        WriteByte(ref platform, buffer, 29, (byte)'R');
        WriteByte(ref platform, buffer, 30, (byte)'M');
        WriteByte(ref platform, buffer, 31, (byte)'A');
        WriteByte(ref platform, buffer, 32, (byte)'T');
        WriteByte(ref platform, buffer, 33, (byte)'/');
        WriteByte(ref platform, buffer, 34, (byte)'K');
        if (profile == EvalCommandProfile.Workbench31)
        {
            WriteByte(ref platform, buffer, 35, 0);
            return 35;
        }
        WriteByte(ref platform, buffer, 35, (byte)',');
        WriteByte(ref platform, buffer, 36, (byte)'H');
        WriteByte(ref platform, buffer, 37, (byte)'E');
        WriteByte(ref platform, buffer, 38, (byte)'X');
        WriteByte(ref platform, buffer, 39, (byte)'/');
        WriteByte(ref platform, buffer, 40, (byte)'S');
        WriteByte(ref platform, buffer, 41, 0);
        return 41;
    }

    private static void WriteByte<TPlatform>(ref TPlatform platform, APTR buffer,
        int offset, byte value) where TPlatform : struct, IShellPlatform =>
        platform.WriteUInt8(buffer, offset, value);
}
