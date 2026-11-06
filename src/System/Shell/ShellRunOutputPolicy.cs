using Amiga;

namespace CopperOS.Shell;

public enum ShellRunOutputDestination
{
    ExplicitNil, Console, Nil,
}

/// <summary>Selects a background command's output and launcher ownership.</summary>
public static class ShellRunOutputPolicy
{
    /// <summary>Chooses the output for a background command.</summary>
    public static ShellRunOutputDestination Destination(bool hasConsole, bool nilOutput) =>
        nilOutput ? ShellRunOutputDestination.ExplicitNil :
        hasConsole ? ShellRunOutputDestination.Console : ShellRunOutputDestination.Nil;

    public static void Select(BPTR output, BPTR error, BPTR standardOutput,
        uint ownedStreams, bool nilOutput, out BPTR childOutput,
        out uint childOwnedStreams, out bool closeLauncherOutput)
    {
        childOutput = nilOutput || standardOutput.IsNull ? output : standardOutput;
        childOwnedStreams = ownedStreams;
        closeLauncherOutput = (ownedStreams & 2) != 0 && childOutput != output;
        if (!closeLauncherOutput) return;
        childOwnedStreams &= ~2u;
        if (error == output)
        {
            // The child still uses the launcher's output as its error stream.
            childOwnedStreams |= 4;
            closeLauncherOutput = false;
        }
    }
}
