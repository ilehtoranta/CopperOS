using Amiga;
using CopperOS.Shell.Dos;
using CopperStart.Dos;
using Xunit;
using F = CopperStart.Exec.Tests.DosChildCompletionCoreTests.Fixture;

namespace CopperStart.Exec.Tests;

public sealed class ShellRomRollbackTests
{
    [Fact]
    public void Failed_publication_releases_image_through_platform_and_preserves_error()
    {
        var f = new F();
        var task = f.PrepareChild();
        var header = f.Platform.AllocateGuest(20);
        var segment = BPTR.FromAddress(APTR.FromPointer(header.Raw + 4));
        DosHunkSegmentHeaderCodec.Write(ref f.Platform, header,
            new() { AllocationSize = 20, Next = BPTR.Null });
        DosCore.SetIoErr(ref f.Platform, F.State, DOS.Error.BadNumber);
        var child = new DosShellPreparedChild
        {
            State = F.State, ExecBase = F.ExecBase, Task = task, Entry = F.Entry,
            Continuation = F.Continuation, CommandArguments = 2, Segment = segment,
        };

        Assert.False(DosShellPreparedChildCore.Publish(ref f.Platform, in child, out var complete));
        Assert.True(complete);
        Assert.Equal((F.State, segment), Assert.Single(f.Memory.DosProcessSegmentUnloads));
        Assert.Equal(DOS.Error.BadNumber, DosCore.IoErr(ref f.Platform, F.State));
        Assert.Empty(f.Memory.DosProcessPublications);
        Assert.False(CopperStart.Dos.DosOwnedProcessTaskCore.IsOwned(ref f.Platform, task));
    }
}
