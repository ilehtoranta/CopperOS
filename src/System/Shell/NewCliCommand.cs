using Amiga;

namespace CopperOS.Shell;

/// <summary>Shell-owned MorphOS <c>NewCLI</c> command.</summary>
public static class NewCliCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR windowBuffer,
        uint windowCapacity,
        APTR fromBuffer,
        uint fromCapacity)
        where TPlatform : struct, IShellPlatform =>
        NewShellCommand.Execute(ref platform, in invocation,
            tokenBuffer, tokenCapacity, windowBuffer, windowCapacity,
            fromBuffer, fromCapacity, ShellLaunchKind.NewCli);
}
