using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class InternalCommandResolverTests
{
    [Theory]
    [InlineData("Echo", ShellInternalCommand.Echo)]
    [InlineData("echo", ShellInternalCommand.Echo)]
    [InlineData("Stack", ShellInternalCommand.Stack)]
    [InlineData("Resident", ShellInternalCommand.Resident)]
    [InlineData("EndShell", ShellInternalCommand.EndShell)]
    [InlineData("Unsetenv", ShellInternalCommand.Unsetenv)]
    [InlineData("Alias", ShellInternalCommand.Alias)]
    [InlineData("Ask", ShellInternalCommand.Ask)]
    [InlineData("CD", ShellInternalCommand.CD)]
    [InlineData("Cls", ShellInternalCommand.Cls)]
    [InlineData("Else", ShellInternalCommand.Else)]
    [InlineData("EndCLI", ShellInternalCommand.EndCLI)]
    [InlineData("EndIf", ShellInternalCommand.EndIf)]
    [InlineData("EndSkip", ShellInternalCommand.EndSkip)]
    [InlineData("Failat", ShellInternalCommand.Failat)]
    [InlineData("Fault", ShellInternalCommand.Fault)]
    [InlineData("Get", ShellInternalCommand.Get)]
    [InlineData("Getenv", ShellInternalCommand.Getenv)]
    [InlineData("If", ShellInternalCommand.If)]
    [InlineData("Lab", ShellInternalCommand.Lab)]
    [InlineData("NewCLI", ShellInternalCommand.NewCLI)]
    [InlineData("NewShell", ShellInternalCommand.NewShell)]
    [InlineData("Path", ShellInternalCommand.Path)]
    [InlineData("Prompt", ShellInternalCommand.Prompt)]
    [InlineData("Quit", ShellInternalCommand.Quit)]
    [InlineData("Run", ShellInternalCommand.Run)]
    [InlineData("Set", ShellInternalCommand.Set)]
    [InlineData("Setenv", ShellInternalCommand.Setenv)]
    [InlineData("Skip", ShellInternalCommand.Skip)]
    [InlineData("Unalias", ShellInternalCommand.Unalias)]
    [InlineData("Unset", ShellInternalCommand.Unset)]
    [InlineData("Why", ShellInternalCommand.Why)]
    public void Resolves_inventory_names_before_filesystem_lookup(
        string name,
        ShellInternalCommand expected)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR address = platform.Store.PutAt(16, name);

        ShellInternalCommand result = ShellInternalCommandResolver.Resolve(
            ref platform,
            address,
            (uint)name.Length);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("NotACommand")]
    [InlineData("")]
    public void Leaves_unknown_names_for_the_next_resolution_stage(string name)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR address = name.Length == 0
            ? APTR.Null
            : platform.Store.PutAt(16, name);

        ShellInternalCommand result = ShellInternalCommandResolver.Resolve(
            ref platform,
            address,
            (uint)name.Length);

        Assert.Equal(ShellInternalCommand.Unknown, result);
    }
}
