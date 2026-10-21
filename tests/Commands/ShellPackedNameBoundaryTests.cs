using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellPackedNameBoundaryTests
{
    [Theory]
    [InlineData("aLiAs", ShellInternalCommand.Alias)]
    [InlineData("CD", ShellInternalCommand.CD)]
    [InlineData("EndShell", ShellInternalCommand.EndShell)]
    [InlineData("Echo", ShellInternalCommand.Echo)]
    public void BoundedNameAtEndOfGuestMemoryNeedsNoPadding(string name, ShellInternalCommand expected)
    {
        var platform = new EchoCommandTests.TestShellPlatform();
        var start = platform.Store.Memory.Length - name.Length;
        for (var i = 0; i < name.Length; i++) platform.Store.Memory[start + i] = (byte)name[i];
        // No terminator or three-byte longword-read padding is supplied by
        // this public bounded-text interface. Alias also begins unaligned.
        Assert.Equal(expected, ShellInternalCommandResolver.Resolve(ref platform,
            APTR.FromPointer((uint)start), (uint)name.Length));
    }
}
