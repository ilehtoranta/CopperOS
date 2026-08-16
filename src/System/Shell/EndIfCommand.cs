using Amiga;

namespace CopperOS.Shell;

public static class EndIfCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity)
        where TPlatform : struct, IShellPlatform =>
        ShellControlCommand.ExecuteNoArguments(
            ref platform, in invocation, tokenBuffer, tokenCapacity,
            ShellControlAction.EndIf);
}
