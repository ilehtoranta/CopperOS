using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ReadArgsCommandSupportTests
{
    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(3u)]
    public void Helper_backed_commands_align_result_slots_for_any_workspace_address(
        uint addressOffset)
    {
        foreach (ShellInternalCommand command in Enum.GetValues<ShellInternalCommand>())
        {
            // Echo supplies its own template/result buffers; every other
            // internal command uses the shared Prepare boundary.
            if (command is ShellInternalCommand.Unknown or ShellInternalCommand.Echo)
                continue;

            EchoCommandTests.TestShellPlatform platform = new();
            CommandInvocation invocation = CreateInvocation(APTR.Null, 0);
            ShellCommandWorkspace workspace = new(
                new APTR(80 + addressOffset), 256,
                new APTR(480 + addressOffset), 256,
                new APTR(800 + addressOffset), 256,
                new APTR(1120 + addressOffset), 256,
                new APTR(1440 + addressOffset), 256,
                new APTR(1760), 512);

            _ = ShellCommandDispatcher.Dispatch(
                ref platform, in invocation, command, in workspace);

            Assert.True(platform.Store.ReadArgsAttemptCount == 1,
                $"{command} did not reach DOS argument parsing.");
            Assert.True(platform.Store.LastReadArgsResultArray.IsNotNull &&
                (platform.Store.LastReadArgsResultArray.Raw & 3u) == 0,
                $"{command} passed unaligned result slots at " +
                $"${platform.Store.LastReadArgsResultArray.Raw:X8}.");
        }
    }

    [Theory]
    [InlineData(80u, 8u)]
    [InlineData(81u, 7u)]
    [InlineData(82u, 6u)]
    [InlineData(83u, 5u)]
    public void Empty_template_accepts_exact_fit_without_touching_guards(
        uint bufferAddress, uint capacity)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        FillGuardedWorkspace(platform.Store, bufferAddress, capacity);
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0);

        int result = ElseCommand.Execute(ref platform, in invocation,
            new APTR(bufferAddress), capacity);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(84u, platform.Store.LastReadArgsResultArray.Raw);
        Assert.Equal(4u, platform.Store.LastReadArgsResultBytes);
        Assert.Equal(1, platform.Store.ControlCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
        AssertGuards(platform.Store, bufferAddress, capacity);
    }

    [Theory]
    [InlineData(80u, 12u, 88u)]
    [InlineData(81u, 11u, 88u)]
    [InlineData(82u, 14u, 92u)]
    [InlineData(83u, 13u, 92u)]
    public void Named_template_accepts_exact_fit_and_preserves_the_value(
        uint bufferAddress, uint capacity, uint resultAddress)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        FillGuardedWorkspace(platform.Store, bufferAddress, capacity);
        platform.Store.LocalVariableName = "N";
        platform.Store.LocalVariableValue = "value";
        APTR source = platform.Store.PutAt(16, "N");
        CommandInvocation invocation = CreateInvocation(source, 1);

        int result = GetCommand.Execute(ref platform, in invocation,
            new APTR(bufferAddress), capacity, new APTR(640), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(resultAddress, platform.Store.LastReadArgsResultArray.Raw);
        Assert.Equal("value\n", platform.Store.OutputText);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
        AssertGuards(platform.Store, bufferAddress, capacity);
    }

    [Theory]
    [InlineData(false, 80u, 7u)]
    [InlineData(false, 81u, 6u)]
    [InlineData(false, 82u, 5u)]
    [InlineData(false, 83u, 4u)]
    [InlineData(true, 80u, 11u)]
    [InlineData(true, 81u, 10u)]
    [InlineData(true, 82u, 13u)]
    [InlineData(true, 83u, 12u)]
    public void One_byte_short_workspace_fails_before_writing_or_calling_DOS(
        bool named, uint bufferAddress, uint capacity)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        FillGuardedWorkspace(platform.Store, bufferAddress, capacity);
        APTR source = named ? platform.Store.PutAt(16, "N") : APTR.Null;
        CommandInvocation invocation = CreateInvocation(source, named ? 1u : 0u);
        byte[] before = (byte[])platform.Store.Memory.Clone();

        int result = named
            ? GetCommand.Execute(ref platform, in invocation,
                new APTR(bufferAddress), capacity, new APTR(640), 64)
            : ElseCommand.Execute(ref platform, in invocation,
                new APTR(bufferAddress), capacity);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.ReadArgsAttemptCount);
        Assert.Equal(0, platform.Store.ControlCount);
        Assert.Equal(before, platform.Store.Memory);
    }

    [Theory]
    [InlineData(uint.MaxValue - 5u, 5u)]
    [InlineData(uint.MaxValue - 4u, 5u)]
    [InlineData(80u, uint.MaxValue)]
    public void Address_or_alignment_wrap_is_rejected_even_for_claimed_mapped_memory(
        uint bufferAddress, uint capacity)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AssumeMemoryMapped = true;
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0);
        byte[] before = (byte[])platform.Store.Memory.Clone();

        int result = ElseCommand.Execute(ref platform, in invocation,
            new APTR(bufferAddress), capacity);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.ReadArgsAttemptCount);
        Assert.Equal(before, platform.Store.Memory);
    }

    private static void FillGuardedWorkspace(
        EchoCommandTests.GuestStore store, uint address, uint capacity) =>
        Array.Fill(store.Memory, (byte)0xA5,
            checked((int)address - 1), checked((int)capacity + 2));

    private static void AssertGuards(
        EchoCommandTests.GuestStore store, uint address, uint capacity)
    {
        Assert.Equal((byte)0xA5, store.Memory[checked((int)address - 1)]);
        Assert.Equal((byte)0xA5, store.Memory[checked((int)(address + capacity))]);
    }

    private static CommandInvocation CreateInvocation(APTR source, uint length) =>
        new(source, length, APTR.Null, APTR.Null, new BPTR(2),
            new BPTR(1), BPTR.Null, BPTR.Null, new APTR(8), 0, 0);
}
