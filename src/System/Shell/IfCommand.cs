using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>If</c> command.
///
/// The documented option template is parsed by DOS.  The anonymous template
/// entry carries the left comparison operand; EQ/GT/GE and EXISTS carry their
/// keyword values.  Comparison and script-frame semantics remain DOS-owned.
/// </summary>
public static class IfCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR leftBuffer,
        uint leftCapacity,
        APTR rightBuffer,
        uint rightCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || tokenBuffer.IsNull ||
            leftBuffer.IsNull || rightBuffer.IsNull || tokenCapacity == 0 ||
            leftCapacity == 0 || rightCapacity == 0 ||
            tokenBuffer.Raw > uint.MaxValue - tokenCapacity ||
            leftBuffer.Raw > uint.MaxValue - leftCapacity ||
            rightBuffer.Raw > uint.MaxValue - rightCapacity ||
            !platform.IsMapped(tokenBuffer, tokenCapacity) ||
            !platform.IsMapped(leftBuffer, leftCapacity) ||
            !platform.IsMapped(rightBuffer, rightCapacity))
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.If,
                IfReadArgsResultRecord.Size,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, IfReadArgsResultRecord.Size,
                out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        if (!IfReadArgsResultRecordCodec.TryRead(ref platform,
                resultArray, out var parsed))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        uint condition = 0;
        uint threshold = 0;
        var thresholdCount = (parsed.Warn != 0 ? 1u : 0u) +
            (parsed.Error != 0 ? 1u : 0u) +
            (parsed.Fail != 0 ? 1u : 0u);
        if (thresholdCount != 0)
        {
            condition = (uint)ShellIfCondition.PreviousResult;
            // MorphOS selects the lowest supplied threshold.
            threshold = parsed.Warn != 0 ? (uint)ShellCommandResult.Warn :
                parsed.Error != 0 ? (uint)ShellCommandResult.Error :
                (uint)ShellCommandResult.Fail;
        }

        var comparisonCount = (parsed.Equal.IsNotNull ? 1u : 0u) +
            (parsed.Greater.IsNotNull ? 1u : 0u) +
            (parsed.GreaterEqual.IsNotNull ? 1u : 0u) +
            (parsed.Exists.IsNotNull ? 1u : 0u);
        if (comparisonCount != 0)
        {
            if (thresholdCount != 0 || comparisonCount != 1)
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }
            condition = parsed.Equal.IsNotNull
                ? (uint)ShellIfCondition.Equal
                : parsed.Greater.IsNotNull
                    ? (uint)ShellIfCondition.Greater
                    : parsed.GreaterEqual.IsNotNull
                        ? (uint)ShellIfCondition.GreaterEqual
                        : (uint)ShellIfCondition.Exists;
        }

        var needsLeft = condition != 0 &&
            condition != (uint)ShellIfCondition.PreviousResult;
        var needsRight = condition is (uint)ShellIfCondition.Equal or
            (uint)ShellIfCondition.Greater or
            (uint)ShellIfCondition.GreaterEqual;
        if (condition == 0 ||
            (condition == (uint)ShellIfCondition.PreviousResult &&
             parsed.Left.IsNotNull) ||
            (condition == (uint)ShellIfCondition.Exists &&
             parsed.Left.IsNotNull) ||
            (needsLeft && parsed.Left.IsNull && parsed.Exists.IsNull) ||
            (needsRight && (parsed.Left.IsNull ||
                (parsed.Equal.IsNull && parsed.Greater.IsNull &&
                 parsed.GreaterEqual.IsNull))))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        uint leftLength = 0;
        uint rightLength = 0;
        if (condition == (uint)ShellIfCondition.Exists)
        {
            if (!ReadArgsCommandSupport.CopyCString(ref platform,
                    parsed.Exists,
                    leftBuffer, leftCapacity, out leftLength))
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }
        }
        else if (needsRight)
        {
            if (!ReadArgsCommandSupport.CopyCString(ref platform,
                    parsed.Left,
                    leftBuffer, leftCapacity, out leftLength))
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }
            var right = APTR.Null;
            if (parsed.Equal.IsNotNull) right = parsed.Equal;
            else if (parsed.Greater.IsNotNull) right = parsed.Greater;
            else right = parsed.GreaterEqual;
            if (!ReadArgsCommandSupport.CopyCString(ref platform, right,
                    rightBuffer, rightCapacity, out rightLength))
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }
        }
        platform.FreeArgs(rdArgs);

        var leftArgument = leftBuffer;
        if (!needsLeft) leftArgument = APTR.FromPointer(0);
        var rightArgument = rightBuffer;
        if (!needsRight) rightArgument = APTR.FromPointer(0);
        var leftArgumentLength = leftLength;
        if (!needsLeft) leftArgumentLength = 0u;
        var rightArgumentLength = rightLength;
        if (!needsRight) rightArgumentLength = 0u;
        return platform.TryEvaluateIf(
                invocation.Cli,
                condition,
                threshold,
                parsed.Not != 0 ? 1u : 0u,
                parsed.NoRequester != 0 ? 1u : 0u,
                parsed.Value != 0 ? 1u : 0u,
                leftArgument,
                leftArgumentLength,
                rightArgument,
                rightArgumentLength)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
