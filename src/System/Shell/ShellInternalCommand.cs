using Amiga;

namespace CopperOS.Shell;

/// <summary>Internal Shell command identities from the frozen MorphOS inventory.</summary>
public enum ShellInternalCommand : int
{
    Unknown = 0, Alias, Ask, CD, Cls, Echo, Else, EndCLI, EndIf, EndShell,
    EndSkip, Failat, Fault, Get, Getenv, If, Lab, NewCLI, NewShell, Path,
    Prompt, Quit, Resident, Run, Set, Setenv, Skip, Stack, Unalias, Unset,
    Unsetenv, Why,
}

/// <summary>Resolves internal command names before resident or filesystem lookup.</summary>
public static class ShellInternalCommandResolver
{
    public static ShellInternalCommand Resolve<TPlatform>(
        ref TPlatform platform, APTR name, uint length)
        where TPlatform : struct, IShellPlatform
    {
        if (name.IsNull || length == 0 || length > 8 ||
            name.Raw > uint.MaxValue - length || !platform.IsMapped(name, length))
            return ShellInternalCommand.Unknown;

        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x416C6961u, 0x73000000u)) return ShellInternalCommand.Alias;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x41736B00u, 0)) return ShellInternalCommand.Ask;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x43440000u, 0)) return ShellInternalCommand.CD;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x436C7300u, 0)) return ShellInternalCommand.Cls;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x4563686Fu, 0)) return ShellInternalCommand.Echo;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x456C7365u, 0)) return ShellInternalCommand.Else;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x456E6443u, 0x4C490000u)) return ShellInternalCommand.EndCLI;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x456E6449u, 0x66000000u)) return ShellInternalCommand.EndIf;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x456E6453u, 0x68656C6Cu)) return ShellInternalCommand.EndShell;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x456E6453u, 0x6B697000u)) return ShellInternalCommand.EndSkip;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x4661696Cu, 0x61740000u)) return ShellInternalCommand.Failat;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x4661756Cu, 0x74000000u)) return ShellInternalCommand.Fault;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x47657400u, 0)) return ShellInternalCommand.Get;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x47657465u, 0x6E760000u)) return ShellInternalCommand.Getenv;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x49660000u, 0)) return ShellInternalCommand.If;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x4C616200u, 0)) return ShellInternalCommand.Lab;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x4E657743u, 0x4C490000u)) return ShellInternalCommand.NewCLI;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x4E657753u, 0x68656C6Cu)) return ShellInternalCommand.NewShell;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x50617468u, 0)) return ShellInternalCommand.Path;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x50726F6Du, 0x70740000u)) return ShellInternalCommand.Prompt;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x51756974u, 0)) return ShellInternalCommand.Quit;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x52657369u, 0x64656E74u)) return ShellInternalCommand.Resident;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x52756E00u, 0)) return ShellInternalCommand.Run;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x53657400u, 0)) return ShellInternalCommand.Set;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x53657465u, 0x6E760000u)) return ShellInternalCommand.Setenv;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x536B6970u, 0)) return ShellInternalCommand.Skip;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x53746163u, 0x6B000000u)) return ShellInternalCommand.Stack;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x556E616Cu, 0x69617300u)) return ShellInternalCommand.Unalias;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x556E7365u, 0x74000000u)) return ShellInternalCommand.Unset;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x556E7365u, 0x74656E76u)) return ShellInternalCommand.Unsetenv;
        if (ShellTextParser.EqualsPacked(ref platform, name, length, 0x57687900u, 0)) return ShellInternalCommand.Why;
        return ShellInternalCommand.Unknown;
    }
}
