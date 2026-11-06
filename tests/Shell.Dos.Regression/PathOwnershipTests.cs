using Amiga;
using CopperOS.Shell;
using CopperOS.Shell.Dos;
using CopperStart.Dos;
using CopperStart.Exec;
using Xunit;

namespace CopperStart.Exec.Tests;

public sealed class PathOwnershipTests
{
    [Fact]
    public void Shell_releases_a_completed_child_using_its_process_owned_frame()
    {
        var fixture = new DosChildCompletionCoreTests.Fixture();
        var frame = fixture.Platform.AllocateGuest(ShellScriptFrameCodec.Size);
        var other = fixture.Platform.AllocateGuest(ShellScriptFrameCodec.Size);
        var shell = new DosShellPlatform<TestMemoryPlatform>(fixture.Platform,
            DosChildCompletionCoreTests.Fixture.State);
        var initial = new ShellScriptFrameState { Cli = fixture.ParentCli,
            Flags = ShellScriptFrameFlags.Active };
        Assert.True(ShellScriptFrameCodec.Initialize(ref shell, frame, in initial));
        Assert.True(ShellScriptFrameCodec.Initialize(ref shell, other, in initial));
        Assert.True(shell.TryBindScriptFrame(fixture.ParentCli, frame));
        var continuation = DosShellStateCore.AllocateScriptRecord(ref fixture.Platform,
            DosChildCompletionCoreTests.Fixture.State, frame, DosChildContinuationCodec.Size, 3);
        var record = new DosChildContinuationRecord { ParentCli = fixture.ParentCli,
            State = DosChildContinuationState.Pending,
            Flags = (uint)DosChildContinuationFlags.RecordOwned };
        Assert.True(DosChildContinuationCodec.Initialize(ref fixture.Platform, continuation, in record));
        var child = DosChildProcessLaunchCore.Prepare<TestMemoryPlatform, ClassicPolicy>(
            ref fixture.Platform, DosChildCompletionCoreTests.Fixture.ExecBase,
            DosChildCompletionCoreTests.Fixture.State, DosChildCompletionCoreTests.Fixture.Tags,
            continuation, fixture.ParentInput, fixture.ParentOutput, BPTR.Null);
        Assert.True(child.IsNotNull);
        Assert.True(DosChildProcessLaunchCore.PublishPrepared(ref fixture.Platform,
            DosChildCompletionCoreTests.Fixture.ExecBase, DosChildCompletionCoreTests.Fixture.State,
            child, DosChildCompletionCoreTests.Fixture.Entry, continuation));
        fixture.ExitAndRetire(child, 10, DOS.Error.BadNumber);
        Assert.True(shell.TryPollShellContinuation(fixture.ParentCli, continuation, out _, out _));
        Assert.True(shell.TryBindScriptFrame(fixture.ParentCli, other));
        Assert.False(shell.TryReleaseShellContinuation(fixture.ParentCli, continuation,
            (uint)DosChildContinuationFlags.RecordOwned));
        Assert.True(shell.TryUnbindScriptFrame(fixture.ParentCli, other));
        Assert.True(shell.TryReleaseShellContinuation(fixture.ParentCli, continuation,
            (uint)DosChildContinuationFlags.RecordOwned));
        Assert.False(shell.TryReleaseShellContinuation(fixture.ParentCli, continuation,
            (uint)DosChildContinuationFlags.RecordOwned));
        Assert.True(shell.TryUnbindScriptFrame(fixture.ParentCli, frame));
        fixture.Platform.FreeGuest(other, ShellScriptFrameCodec.Size);
        fixture.Platform.FreeGuest(frame, ShellScriptFrameCodec.Size);
    }

    [Fact]
    public void Shell_frame_owns_controls_and_labels_and_reclaims_unfinished_blocks()
    {
        var task = new APTR(0x2700); var cli = new APTR(0x8000);
        var state = new APTR(0x1000); var name = new APTR(0x2300);
        var memory = new GuestMemory(0x40000) { DosNextAllocation = 0x10000, DosCurrentTask = task };
        var dos = new TestMemoryPlatform(memory);
        DosCore.Initialize(ref dos, state, BPTR.Null);
        DosProcessCodec.WriteCommandLineInterface(ref dos, task, BPTR.FromAddress(cli));
        DosProcessCodec.WriteShellPrivate(ref dos, task, DosSystemCore.ExecuteShellPrivate);
        var frame = dos.AllocateGuest(ShellScriptFrameCodec.Size);
        var shell = new DosShellPlatform<TestMemoryPlatform>(dos, state);
        var initial = new ShellScriptFrameState { Cli = cli, Parent = cli,
            Flags = ShellScriptFrameFlags.Active, FailureLimit = 10 };
        Assert.True(ShellScriptFrameCodec.Initialize(ref shell, frame, in initial));
        Assert.True(shell.TryBindScriptFrame(cli, frame));
        Assert.False(DosShellNativeBridge.TryGetScriptFrame(ref dos, state, cli, out _));
        Assert.True(ShellRunnerBinding.TryFind(ref shell, cli, out var binding, out var boundFrame, out var boundRunner));
        Assert.True(binding.IsNotNull && boundFrame == frame && boundRunner.IsNull);
        Write(ref dos, name, "mark");
        Assert.True(shell.TryDefineScriptLabel(cli, name, 4));
        memory.DosFailGuestAllocationSize = 16 + ShellScriptControlCodec.Size;
        memory.DosFailGuestAllocationsRemaining = 1;
        Assert.False(shell.TryEvaluateIf(cli, (uint)ShellIfCondition.PreviousResult,
            0, 0, 0, 0, APTR.Null, 0, APTR.Null, 0));
        Assert.Equal(DOS.Error.NoFreeStore, DosCore.IoErr(ref dos, state));
        Assert.True(ShellScriptFrameCodec.TryRead(ref shell, frame, out var afterFailure));
        Assert.True(afterFailure.ControlTop.IsNull);
        Assert.True(shell.TryEvaluateIf(cli, (uint)ShellIfCondition.PreviousResult,
            0, 0, 0, 0, APTR.Null, 0, APTR.Null, 0));
        Assert.True(shell.TryEvaluateIf(cli, (uint)ShellIfCondition.PreviousResult,
            0, 0, 0, 0, APTR.Null, 0, APTR.Null, 0));
        Assert.True(ShellScriptFrameCodec.TryRead(ref shell, frame, out var current));
        var label = current.LabelTop;
        var inner = current.ControlTop;
        Assert.True(label.IsNotNull && inner.IsNotNull);
        Assert.False(DosShellNativeBridge.FreeScriptRecord(ref dos, state, frame, label, 2));
        Assert.False(DosShellNativeBridge.FreeScriptRecord(ref dos, state, frame, inner, 1));
        Assert.True(shell.TryRequestShellControl(cli, ShellControlAction.EndIf, 0));
        Assert.Contains((new APTR(inner.Raw - 16), 16u + ShellScriptControlCodec.Size), memory.UtilityFrees);
        Assert.True(shell.TryUnbindScriptFrame(cli, frame));
        Assert.Equal(DosSystemCore.ExecuteShellPrivate, DosProcessCodec.ReadShellPrivate(ref dos, task));
        Assert.Contains((new APTR(label.Raw - 16), 16u + ShellScriptLabelCodec.Size + 4), memory.UtilityFrees);
        Assert.Equal(2, memory.UtilityFrees.Count(item => item.Size == 16 + ShellScriptControlCodec.Size));
        Assert.True(ShellFrameResources.CanRelease(ref shell, frame));
        dos.FreeGuest(frame, ShellScriptFrameCodec.Size);
    }

    [Fact]
    public void Shared_shell_script_runner_restores_process_field_after_completion()
    {
        var task = new APTR(0x2700); var cli = new APTR(0x8000);
        var state = new APTR(0x1000); var name = new APTR(0x2300);
        var memory = new GuestMemory(0x40000) { DosNextAllocation = 0x10000,
            DosCurrentTask = task, DosFileLength = 1 };
        memory.DosFile[0] = (byte)'\n';
        var dos = new TestMemoryPlatform(memory);
        DosCore.Initialize(ref dos, state, BPTR.Null);
        Assert.True(DosCore.CreateProcessState(ref dos, state, task,
            BPTR.Null, BPTR.Null, BPTR.Null, out _, out _, out _));
        DosProcessCodec.WriteCommandLineInterface(ref dos, task, BPTR.FromAddress(cli));
        DosProcessCodec.WriteShellPrivate(ref dos, task, DosSystemCore.ExecuteShellPrivate);
        Write(ref dos, name, "script\0");
        var output = DosCore.Open(ref dos, state, name, DOS.FileMode.OldFile);
        Assert.True(output.IsNotNull);
        DosCommandLineInterfaceCodec.Write(ref dos, cli, new CommandLineInterface
        {
            StandardInput = output, CurrentInput = output, StandardOutput = output,
            CurrentOutput = output, FailLevel = 10,
        });
        var shell = new DosShellPlatform<TestMemoryPlatform>(dos, state);
        var status = shell.TryExecuteScript(cli, name, 6, APTR.Null, 0, out var result);
        Assert.Equal(ShellScriptExecutionStatus.Completed, status);
        Assert.Equal(0, result);
        Assert.Equal(DosSystemCore.ExecuteShellPrivate, DosProcessCodec.ReadShellPrivate(ref dos, task));
        Assert.Contains(memory.UtilityFrees, item => item.Size == ShellRunnerBuffers.RecordSize);
        Assert.Contains(memory.UtilityFrees, item => item.Size == ShellRunnerBinding.Size);
        Assert.True(ShellRunnerBinding.TryFind(ref shell, cli, out var binding, out _, out _));
        Assert.True(binding.IsNull);
        var closed = memory.DosCloseCount;
        var priorRunnerFrees = memory.UtilityFrees.Count(item => item.Size == ShellRunnerBuffers.RecordSize);
        memory.DosFailGuestAllocationSize = ShellRunnerBinding.Size;
        memory.DosFailGuestAllocationsRemaining = 1;
        status = shell.TryExecuteScript(cli, name, 6, APTR.Null, 0, out result);
        Assert.Equal(ShellScriptExecutionStatus.Failed, status);
        Assert.Equal(0, memory.DosFailGuestAllocationsRemaining);
        Assert.Equal(closed + 1, memory.DosCloseCount);
        Assert.Equal(priorRunnerFrees + 1, memory.UtilityFrees.Count(item => item.Size == ShellRunnerBuffers.RecordSize));
        Assert.Equal(DOS.Error.NoFreeStore, DosCore.IoErr(ref dos, state));
        Assert.Equal(DosSystemCore.ExecuteShellPrivate, DosProcessCodec.ReadShellPrivate(ref dos, task));
        Assert.True(DosShellNativeBridge.FindScriptRunner(ref dos, state, cli).IsNull);
        Assert.True(ShellRunnerBinding.TryFind(ref shell, cli, out binding, out _, out _));
        Assert.True(binding.IsNull);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Shell_cleanup_retires_dos_runner_and_its_owned_storage(bool packed)
    {
        var task = new APTR(0x2700); var cli = new APTR(0x8000);
        var memory = new GuestMemory(0x40000) { DosNextAllocation = 0x10000, DosCurrentTask = task };
        var dos = new TestMemoryPlatform(memory);
        var state = new APTR(0x1000);
        DosCore.Initialize(ref dos, state, BPTR.Null);
        memory.UtilityFrees.Clear();
        var frame = dos.AllocateGuest(ShellScriptFrameCodec.Size);
        DosProcessCodec.WriteCommandLineInterface(ref dos, task, BPTR.FromAddress(cli));
        DosProcessCodec.WriteShellPrivate(ref dos, task, DosSystemCore.ExecuteShellPrivate);
        var value = new DosShellScriptRunnerRecord { Cli = cli, Frame = frame,
            Input = BPTR.FromRaw(1), State = DosShellScriptRunnerState.Running };
        APTR runner;
        if (packed)
        {
            runner = DosShellScriptRunnerCore.AllocateWorkspace(ref dos, state, frame, out var workspace);
            Fill(ref value, workspace);
            Assert.True(DosShellScriptRunnerCore.InitializeWorkspace(ref dos, state, runner, in value));
        }
        else
        {
            value.Line = dos.AllocateGuest(4096); value.CommandName = dos.AllocateGuest(256);
            value.Token = dos.AllocateGuest(4096); value.First = dos.AllocateGuest(512);
            value.Second = dos.AllocateGuest(512); value.Third = dos.AllocateGuest(512);
            value.Fourth = dos.AllocateGuest(512); value.RedirectionCommand = dos.AllocateGuest(4096);
            value.RedirectionInput = dos.AllocateGuest(512); value.RedirectionOutput = dos.AllocateGuest(512);
            value.RedirectionError = dos.AllocateGuest(512); value.AliasLine = dos.AllocateGuest(4096);
            value.LookupPath = dos.AllocateGuest(512); value.ScriptTemporaryPath = dos.AllocateGuest(512);
            runner = DosShellScriptRunnerCore.Allocate(ref dos, state, frame, in value);
        }
        Assert.True(DosShellNativeBridge.ReadScriptRunner(ref dos, state, runner, out var stored));
        var shell = new DosShellPlatform<TestMemoryPlatform>(dos, state);
        Assert.True(ShellRunnerBinding.TryPublish(ref shell, cli, frame, runner));
        Assert.True(ShellRunnerBinding.TryFind(ref shell, cli, out _, out var boundFrame, out var boundRunner));
        Assert.Equal(frame, boundFrame); Assert.Equal(runner, boundRunner);
        Assert.True(ShellRunnerBinding.TryRemove(ref shell, cli, frame, runner));
        Assert.Equal(DosSystemCore.ExecuteShellPrivate, DosProcessCodec.ReadShellPrivate(ref dos, task));
        Assert.True(ShellRunnerCleanup.Release(ref shell, runner, packed));
        Assert.False(DosShellNativeBridge.ReadScriptRunner(ref dos, state, runner, out _));
        Assert.True(DosShellNativeBridge.FreeScriptRecord(ref dos, state, stored.Frame, runner, 4));
        Assert.Equal(packed ? 3 : 17, memory.UtilityFrees.Count);
        Assert.Contains((frame, ShellScriptFrameCodec.Size), memory.UtilityFrees);
        Assert.Equal(0u, dos.ReadUInt32(state, 88));
    }

    [Fact]
    public void Shared_shell_path_updates_are_visible_to_dos_and_reclaimed_by_the_cli()
    {
        var state = new APTR(0x1000);
        var task = new APTR(0x2700);
        var cli = new APTR(0x8000);
        var names = new APTR(0x2300);
        var memory = new GuestMemory(0x40000)
        {
            DosFileLength64 = 1, DosCurrentTask = task,
            DosExaminedFileType = (int)DosConstants.UserDirectory,
        };
        var dos = new TestMemoryPlatform(memory);
        DosCore.Initialize(ref dos, state, BPTR.Null);
        Assert.True(DosCore.CreateProcessState(ref dos, state, task,
            BPTR.Null, BPTR.Null, BPTR.Null, out _, out _, out _));
        DosCommandLineInterfaceCodec.Write(ref dos, cli, default);
        DosProcessCodec.WriteCommandLineInterface(ref dos, task, BPTR.FromAddress(cli));
        Write(ref dos, names, "SYS:C\0");
        var shell = new DosShellPlatform<TestMemoryPlatform>(dos, state);
        Assert.True(shell.TryUpdateCommandPath(cli, names, 6, 1, PathCommand.OperationReset, 1));
        Assert.True(DosCore.TryGetShellProcessContext(ref dos, state, cli, out var context));
        var path = context.CommandPath;
        Assert.True(path.IsNotNull);
        var first = DosPathLockCodec.Read(ref dos, path.Address);
        Assert.Equal(PathLock.Size + 4, dos.ReadUInt32(APTR.FromPointer(path.Address.Raw - 4)));
        Assert.True(first.Lock.IsNotNull);
        Assert.True(first.Next.IsNull);
        Assert.Equal(path, DosCommandLineInterfaceCodec.ReadCommandDirectory(ref dos, cli));

        memory.DosFileLength64 = 0;
        Write(ref dos, names, "Missing:C\0");
        Assert.False(shell.TryUpdateCommandPath(cli, names, 10, 1, PathCommand.OperationAdd, 1));
        Assert.Equal(path, DosCommandLineInterfaceCodec.ReadCommandDirectory(ref dos, cli));
        Assert.NotEqual(DOS.Error.None, DosCore.IoErr(ref dos, state));
        memory.DosFileLength64 = 1;
        Assert.True(DosCore.TryReleaseProcessState(ref dos, state, task));
        Assert.True(DosCore.DupLock(ref dos, state, first.Lock).IsNull);
        Assert.Single(memory.UtilityFrees, item => item.Address == APTR.FromPointer(path.Address.Raw - 4) && item.Size == PathLock.Size + 4);
    }

    private static void Write(ref TestMemoryPlatform platform, APTR address, string text)
    {
        for (var i = 0; i < text.Length; i++) platform.WriteUInt8(address, i, (byte)text[i]);
    }

    private static void Fill(ref DosShellScriptRunnerRecord value, APTR start)
    {
        value.Line = start; value.CommandName = At(start, ShellScriptWorkspaceLayout.Name);
        value.Token = At(start, ShellScriptWorkspaceLayout.Token); value.First = At(start, ShellScriptWorkspaceLayout.First);
        value.Second = At(start, ShellScriptWorkspaceLayout.Second); value.Third = At(start, ShellScriptWorkspaceLayout.Third);
        value.Fourth = At(start, ShellScriptWorkspaceLayout.Fourth);
        value.RedirectionCommand = At(start, ShellScriptWorkspaceLayout.Redirection);
        value.RedirectionInput = At(start, ShellScriptWorkspaceLayout.Input);
        value.RedirectionOutput = At(start, ShellScriptWorkspaceLayout.Output);
        value.RedirectionError = At(start, ShellScriptWorkspaceLayout.Error);
        value.AliasLine = At(start, ShellScriptWorkspaceLayout.Alias);
        value.LookupPath = At(start, ShellScriptWorkspaceLayout.Lookup);
        value.ScriptTemporaryPath = At(start, ShellScriptWorkspaceLayout.Temporary);
    }

    private static APTR At(APTR start, uint offset) => APTR.FromPointer(start.Raw + offset);
}
