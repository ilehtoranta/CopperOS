using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands;

/// <summary>
/// Bounded external MorphOS Quote command boundary. It delegates command-line
/// grammar to DOS ReadArgs and composes only the independently implemented
/// rule stages. FILE, FIRSTLINE, and NOQUOTES remain explicit incomplete
/// modes until their DOS/reference contracts are available.
/// </summary>
public static class QuoteCommand
{
    private const uint ResultSlots = 8;
    private const uint MaximumTextBytes = 4096;

    public static int Execute<TPlatform>(ref TPlatform platform,
        in CommandInvocation invocation, APTR template, uint templateCapacity,
        APTR results, uint resultsCapacity, APTR input, uint inputCapacity,
        APTR ruleList, uint ruleListCapacity, APTR first, uint firstCapacity,
        APTR second, uint secondCapacity, APTR output, uint outputCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (template.IsNull || results.IsNull || input.IsNull || ruleList.IsNull ||
            first.IsNull || second.IsNull || output.IsNull || templateCapacity < 74 ||
            resultsCapacity < ResultSlots * 4 || inputCapacity == 0 ||
            ruleListCapacity == 0 || firstCapacity == 0 || secondCapacity == 0 ||
            outputCapacity == 0 || !platform.IsMapped(template, 74) ||
            !platform.IsMapped(results, ResultSlots * 4) ||
            !platform.IsMapped(input, inputCapacity) ||
            !platform.IsMapped(ruleList, ruleListCapacity) ||
            !platform.IsMapped(first, firstCapacity) ||
            !platform.IsMapped(second, secondCapacity) ||
            !platform.IsMapped(output, outputCapacity))
            return (int)ShellCommandResult.Fail;

        WriteTemplate(ref platform, template);
        for (var slot = 0u; slot < ResultSlots; slot++)
            platform.WriteUInt32(results, (int)(slot * 4), 0);
        if (!platform.TryReadArgs(invocation.ArgumentText, invocation.ArgumentLength,
                template, 73, results, ResultSlots * 4, out var rdArgs))
            return (int)ShellCommandResult.Error;

        try
        {
            var rules = APTR.FromPointer(platform.ReadUInt32(results, 0));
            var file = APTR.FromPointer(platform.ReadUInt32(results, 4));
            var variable = APTR.FromPointer(platform.ReadUInt32(results, 8));
            var text = APTR.FromPointer(platform.ReadUInt32(results, 12));
            var noLine = platform.ReadUInt32(results, 16) != 0;
            var noQuotes = platform.ReadUInt32(results, 20) != 0;
            var firstLine = platform.ReadUInt32(results, 24) != 0;
            var reverse = platform.ReadUInt32(results, 28) != 0;
            if (rules.IsNull || file.IsNotNull || noQuotes || firstLine ||
                !CStringCodec.TryReadLength(ref platform, rules,
                    MaximumTextBytes, out var rulesLength) ||
                !QuoteRuleParser.TryParse(ref platform, rules, rulesLength,
                    ruleList, ruleListCapacity, out var ruleCount))
                return (int)ShellCommandResult.Error;

            uint inputLength;
            if (text.IsNotNull)
            {
                if (!CStringCodec.TryReadLength(ref platform, text,
                        MaximumTextBytes, out inputLength) || inputLength > inputCapacity)
                    return (int)ShellCommandResult.Error;
                platform.Copy(text, input, inputLength);
            }
            else if (variable.IsNotNull)
            {
                if (!CStringCodec.TryReadLength(ref platform, variable,
                        MaximumTextBytes, out var variableLength) ||
                    !(platform.TryGetLocalVariable(invocation.Cli, variable,
                        variableLength, input, inputCapacity, out inputLength) ||
                      platform.TryGetGlobalVariable(variable, variableLength,
                        input, inputCapacity, out inputLength)))
                    return (int)ShellCommandResult.Error;
            }
            else return (int)ShellCommandResult.Error;

            bool transformed = reverse
                ? QuoteReversePipeline.TryApply(ref platform, input, inputLength,
                    ruleList, ruleCount, first, firstCapacity, second,
                    secondCapacity, out var transformedText, out var transformedLength)
                : QuoteForwardPipeline.TryApply(ref platform, input, inputLength,
                    ruleList, ruleCount, first, firstCapacity, second,
                    secondCapacity, out transformedText, out transformedLength);
            if (!transformed || transformedLength > outputCapacity - (noLine ? 0u : 1u))
                return (int)ShellCommandResult.Error;
            platform.Copy(transformedText, output, transformedLength);
            var written = transformedLength;
            if (!noLine) platform.WriteUInt8(output, (int)written++, (byte)'\n');
            return platform.Write(invocation.Output, output, written) ==
                (int)written ? (int)ShellCommandResult.Ok : (int)ShellCommandResult.Error;
        }
        finally { platform.FreeArgs(rdArgs); }
    }

    private static void WriteTemplate<TPlatform>(ref TPlatform platform,
        APTR destination) where TPlatform : struct, IShellPlatform
    {
        platform.WriteUInt32(destination, 0, 0x52554C45);  // RULE
        platform.WriteUInt32(destination, 4, 0x2F412C46);  // /A,F
        platform.WriteUInt32(destination, 8, 0x494C452F);  // ILE/
        platform.WriteUInt32(destination, 12, 0x4B2C5641); // K,VA
        platform.WriteUInt32(destination, 16, 0x522F4B2C); // R/K,
        platform.WriteUInt32(destination, 20, 0x5354522C); // STR,
        platform.WriteUInt32(destination, 24, 0x4E4F4C49); // NOLI
        platform.WriteUInt32(destination, 28, 0x4E452F53); // NE/S
        platform.WriteUInt32(destination, 32, 0x2C4E4F51); // ,NOQ
        platform.WriteUInt32(destination, 36, 0x554F5445); // UOTE
        platform.WriteUInt32(destination, 40, 0x532F532C); // S/S,
        platform.WriteUInt32(destination, 44, 0x46495253); // FIRS
        platform.WriteUInt32(destination, 48, 0x544C494E); // TLIN
        platform.WriteUInt32(destination, 52, 0x452F532C); // E/S,
        platform.WriteUInt32(destination, 56, 0x52455645); // REVE
        platform.WriteUInt32(destination, 60, 0x5253453D); // RSE=
        platform.WriteUInt32(destination, 64, 0x554E5155); // UNQU
        platform.WriteUInt32(destination, 68, 0x4F54452F); // OTE/
        platform.WriteUInt8(destination, 72, (byte)'S');
        platform.WriteUInt8(destination, 73, 0);
    }
}
