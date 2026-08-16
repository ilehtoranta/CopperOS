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
                tokenCapacity, ReadArgsCommandTemplate.If, 44,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, 44, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var not = platform.ReadUInt32(resultArray);
        var warn = platform.ReadUInt32(resultArray, 4);
        var error = platform.ReadUInt32(resultArray, 8);
        var fail = platform.ReadUInt32(resultArray, 12);
        var left = APTR.FromPointer(platform.ReadUInt32(resultArray, 16));
        var equal = APTR.FromPointer(platform.ReadUInt32(resultArray, 20));
        var greater = APTR.FromPointer(platform.ReadUInt32(resultArray, 24));
        var greaterEqual = APTR.FromPointer(platform.ReadUInt32(resultArray, 28));
        var value = platform.ReadUInt32(resultArray, 32);
        var exists = APTR.FromPointer(platform.ReadUInt32(resultArray, 36));
        var noRequester = platform.ReadUInt32(resultArray, 40);

        uint condition = 0;
        uint threshold = 0;
        var thresholdCount = (warn != 0 ? 1u : 0u) +
            (error != 0 ? 1u : 0u) + (fail != 0 ? 1u : 0u);
        if (thresholdCount != 0)
        {
            condition = (uint)ShellIfCondition.PreviousResult;
            // MorphOS selects the lowest supplied threshold.
            threshold = warn != 0 ? (uint)ShellCommandResult.Warn :
                error != 0 ? (uint)ShellCommandResult.Error :
                (uint)ShellCommandResult.Fail;
        }

        var comparisonCount = (equal.IsNotNull ? 1u : 0u) +
            (greater.IsNotNull ? 1u : 0u) +
            (greaterEqual.IsNotNull ? 1u : 0u) +
            (exists.IsNotNull ? 1u : 0u);
        if (comparisonCount != 0)
        {
            if (thresholdCount != 0 || comparisonCount != 1)
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }
            condition = equal.IsNotNull
                ? (uint)ShellIfCondition.Equal
                : greater.IsNotNull
                    ? (uint)ShellIfCondition.Greater
                    : greaterEqual.IsNotNull
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
             left.IsNotNull) ||
            (condition == (uint)ShellIfCondition.Exists &&
             left.IsNotNull) ||
            (needsLeft && left.IsNull && exists.IsNull) ||
            (needsRight && (left.IsNull ||
                (equal.IsNull && greater.IsNull && greaterEqual.IsNull))))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        uint leftLength = 0;
        uint rightLength = 0;
        if (condition == (uint)ShellIfCondition.Exists)
        {
            if (!ReadArgsCommandSupport.CopyCString(ref platform, exists,
                    leftBuffer, leftCapacity, out leftLength))
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }
        }
        else if (needsRight)
        {
            if (!ReadArgsCommandSupport.CopyCString(ref platform, left,
                    leftBuffer, leftCapacity, out leftLength))
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }
            var right = APTR.FromPointer(0);
            if (equal.IsNotNull) right = equal;
            else if (greater.IsNotNull) right = greater;
            else right = greaterEqual;
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
                not != 0 ? 1u : 0u,
                noRequester != 0 ? 1u : 0u,
                value != 0 ? 1u : 0u,
                leftArgument,
                leftArgumentLength,
                rightArgument,
                rightArgumentLength)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
