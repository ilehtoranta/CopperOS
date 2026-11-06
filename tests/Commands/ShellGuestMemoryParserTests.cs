using System.Text;
using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellGuestMemoryParserTests
{
    [Theory]
    [InlineData(true, "T:Prompt.12345678")]
    [InlineData(false, ":T/Prompt.12345678")]
    public void Prompt_capture_uses_assigned_t_or_the_volume_t_directory(bool assigned, string expected)
    {
        var memory = new PathMemory();
        var path = new APTR(1024);
        Encoding.ASCII.GetBytes("T:Prompt.12345678\0").CopyTo(memory.Memory.Bytes, (int)path.Raw);
        if (assigned) memory.State.Names.Add("T:", 1);
        var length = 17u;
        Assert.True(ShellScriptTemporaryPath.TrySelectPromptDirectory(ref memory, path, 64, ref length));
        Assert.Equal(expected, Encoding.ASCII.GetString(memory.Memory.Bytes, (int)path.Raw, (int)length));
        Assert.Equal(0, memory.ReadUInt8(path, (int)length));
        Assert.Equal(1, memory.State.RequesterEnters);
        Assert.Equal(1, memory.State.RequesterExits);
        Assert.Equal(assigned ? 1 : 0, memory.State.ReleasedLocks.Count);
        Assert.Equal(0, memory.State.PathError);
    }

    [Fact]
    public void Prompt_capture_does_not_fallback_when_path_inspection_runs_out_of_memory()
    {
        var memory = new PathMemory();
        var path = new APTR(1024);
        Encoding.ASCII.GetBytes("T:Prompt.12345678\0").CopyTo(memory.Memory.Bytes, (int)path.Raw);
        memory.State.LockFailure = (int)DOS.Error.NoFreeStore;
        var length = 17u;
        Assert.False(ShellScriptTemporaryPath.TrySelectPromptDirectory(ref memory, path, 64, ref length));
        Assert.Equal("T:Prompt.12345678", Encoding.ASCII.GetString(memory.Memory.Bytes, (int)path.Raw, (int)length));
        Assert.Equal((int)DOS.Error.NoFreeStore, memory.State.PathError);
        Assert.Equal(1, memory.State.RequesterExits);
    }

    [Fact]
    public void Command_source_owns_text_and_reads_lines_with_fgets_cursors()
    {
        var memory = new PathMemory();
        var text = new APTR(1024);
        var destination = new APTR(2048);
        Encoding.ASCII.GetBytes("ONE\r\nTWO\n").CopyTo(memory.Memory.Bytes, (int)text.Raw);
        var source = ShellCommandSource.Create(ref memory, text, 9);
        Assert.True(ShellCommandSource.IsValid(ref memory, source));
        memory.WriteUInt8(text, 0, (byte)'X'); // the reader owns a copy
        Assert.True(ShellCommandSource.TryRead(ref memory, source, 0, destination, 16,
            out var length, out var offset, out var eof));
        Assert.Equal((3u, 5u, 0u), (length, offset, eof));
        Assert.Equal("ONE", Encoding.ASCII.GetString(memory.Memory.Bytes, (int)destination.Raw, (int)length));
        Assert.True(ShellCommandSource.TryRead(ref memory, source, offset, destination, 16,
            out length, out offset, out eof));
        Assert.Equal((3u, 9u, 0u), (length, offset, eof));
        Assert.Equal("TWO", Encoding.ASCII.GetString(memory.Memory.Bytes, (int)destination.Raw, (int)length));
        Assert.True(ShellCommandSource.TryRead(ref memory, source, offset, destination, 16,
            out length, out offset, out eof));
        Assert.Equal((0u, 10u, 0u), (length, offset, eof));
        Assert.True(ShellCommandSource.TryRead(ref memory, source, offset, destination, 16,
            out length, out offset, out eof));
        Assert.Equal((0u, 10u, 1u), (length, offset, eof));
        Assert.True(ShellCommandSource.TryRead(ref memory, source, 0, destination, 3,
            out length, out offset, out eof));
        Assert.Equal((2u, 2u, 0u), (length, offset, eof));
        Assert.False(ShellCommandSource.TryRead(ref memory, source, 0, destination, 1,
            out _, out _, out _));
        Assert.True(ShellCommandSource.Release(ref memory, source));
        Assert.Contains((source.Address.Raw, 18u), memory.State.ReleasedMemory);
        Assert.False(ShellCommandSource.Release(ref memory, source));
    }

    [Fact]
    public void Runner_cleanup_frees_shell_owned_command_source_without_closing_a_dos_handle()
    {
        var memory = new PathMemory();
        var text = new APTR(1024);
        Encoding.ASCII.GetBytes("ECHO test").CopyTo(memory.Memory.Bytes, (int)text.Raw);
        var source = ShellCommandSource.Create(ref memory, text, 9);
        var runner = new APTR(4096);
        memory.WriteUInt32(runner, 0, ShellScriptRunnerCodec.Magic);
        memory.WriteUInt32(runner, 4, ShellScriptRunnerCodec.Version);
        memory.WriteUInt32(runner, 16, source.Raw);
        memory.WriteUInt32(runner, 88, 2);
        Assert.True(ShellRunnerCleanup.Release(ref memory, runner, false));
        Assert.Contains((source.Address.Raw, 18u), memory.State.ReleasedMemory);
        Assert.DoesNotContain(memory.State.CleanupEvents, item => item.StartsWith("close:"));
        Assert.False(ShellRunnerCleanup.Release(ref memory, runner, false));
    }

    [Fact]
    public void Frame_owns_and_releases_nested_controls_and_labels_atomically()
    {
        var memory = new PathMemory();
        var frame = new APTR(512);
        var other = new APTR(1024);
        var initial = new ShellScriptFrameState { Cli = new APTR(256), Flags = ShellScriptFrameFlags.Active };
        Assert.True(ShellScriptFrameCodec.Initialize(ref memory, frame, in initial));
        Assert.True(ShellScriptFrameCodec.Initialize(ref memory, other, in initial));
        var first = ShellFrameResources.AllocateControl(ref memory, frame);
        var second = ShellFrameResources.AllocateControl(ref memory, frame);
        var label = ShellFrameResources.AllocateLabel(ref memory, frame, 5, out var name);
        Assert.True(first.IsNotNull && second.IsNotNull && label.IsNotNull && name.IsNotNull);
        foreach (var block in new[] { first, second })
        {
            memory.WriteUInt32(block, 0, ShellScriptControlCodec.Magic);
            memory.WriteUInt32(block, 4, ShellScriptControlCodec.Version);
            memory.WriteUInt32(block, 12, (uint)ShellScriptBlockKind.If);
        }
        memory.WriteUInt32(second, 8, first.Raw);
        memory.WriteUInt32(frame, 68, second.Raw);
        memory.WriteUInt32(label, 0, ShellScriptLabelCodec.Magic);
        memory.WriteUInt32(label, 4, ShellScriptLabelCodec.Version);
        memory.WriteUInt32(label, 12, name.Raw);
        memory.WriteUInt32(label, 16, 5);
        memory.WriteUInt32(frame, 64, 1);
        memory.WriteUInt32(frame, 76, label.Raw);
        Assert.False(ShellFrameResources.FreeControl(ref memory, other, second));
        Assert.True(ShellFrameResources.CanRelease(ref memory, frame));

        // A malformed list must not detach or free any of the valid records.
        memory.WriteUInt32(first, 8, second.Raw);
        Assert.False(ShellFrameResources.CanRelease(ref memory, frame));
        memory.WriteUInt32(first, 8, 0);
        memory.WriteUInt32(label, 8, label.Raw);
        var before = (byte[])memory.Memory.Bytes.Clone();
        Assert.False(ShellFrameResources.Release(ref memory, frame));
        Assert.Equal(before, memory.Memory.Bytes);
        Assert.Empty(memory.State.ReleasedMemory);
        memory.WriteUInt32(label, 8, 0);
        Assert.True(ShellFrameResources.Release(ref memory, frame));
        Assert.Equal(0u, memory.ReadUInt32(frame, 64));
        Assert.Equal(0u, memory.ReadUInt32(frame, 68));
        Assert.Equal(0u, memory.ReadUInt32(frame, 76));
        Assert.Contains((first.Raw - 16, 16 + ShellScriptControlCodec.Size), memory.State.ReleasedMemory);
        Assert.Contains((second.Raw - 16, 16 + ShellScriptControlCodec.Size), memory.State.ReleasedMemory);
        Assert.Contains((label.Raw - 16, 16 + ShellScriptLabelCodec.Size + 5), memory.State.ReleasedMemory);
        Assert.False(ShellFrameResources.FreeLabel(ref memory, frame, label));
        memory.State.FailAllocation = true;
        Assert.True(ShellFrameResources.AllocateControl(ref memory, frame).IsNull);
        Assert.True(ShellFrameResources.AllocateLabel(ref memory, frame, 3, out _).IsNull);
    }

    [Fact]
    public void Frame_continuation_allocation_checks_ownership_and_reclaims_command_storage()
    {
        var memory = new PathMemory();
        var frame = new APTR(512);
        var other = new APTR(1024);
        var initial = new ShellScriptFrameState { Cli = new APTR(256), Flags = ShellScriptFrameFlags.Active };
        Assert.True(ShellScriptFrameCodec.Initialize(ref memory, frame, in initial));
        Assert.True(ShellScriptFrameCodec.Initialize(ref memory, other, in initial));
        var record = ShellFrameResources.AllocateContinuation(ref memory, frame, 12, out var command);
        Assert.True(record.IsNotNull);
        Assert.Equal(record.Raw + ShellProcessContinuationCodec.Size, command.Raw);
        Assert.True(ShellFrameResources.IsContinuationOwned(ref memory, frame, record));
        Assert.False(ShellFrameResources.IsContinuationOwned(ref memory, other, record));
        Assert.False(ShellFrameResources.FreeContinuation(ref memory, other, record));
        Assert.Empty(memory.State.ReleasedMemory);
        Assert.True(ShellFrameResources.FreeContinuation(ref memory, frame, record));
        Assert.Contains((record.Raw - 16, 16 + ShellProcessContinuationCodec.Size + 12), memory.State.ReleasedMemory);
        Assert.False(ShellFrameResources.FreeContinuation(ref memory, frame, record));
        var empty = ShellFrameResources.AllocateContinuation(ref memory, frame, 0, out var noCommand);
        Assert.True(empty.IsNotNull);
        Assert.True(noCommand.IsNull);
        memory.WriteUInt32(APTR.FromPointer(empty.Raw - 16), 12, 16 + ShellProcessContinuationCodec.Size - 1);
        Assert.False(ShellFrameResources.IsContinuationOwned(ref memory, frame, empty));
        Assert.False(ShellFrameResources.FreeContinuation(ref memory, frame, empty));
        memory.WriteUInt32(APTR.FromPointer(empty.Raw - 16), 12, 16 + ShellProcessContinuationCodec.Size);
        Assert.True(ShellFrameResources.FreeContinuation(ref memory, frame, empty));
        Assert.True(ShellFrameResources.AllocateContinuation(ref memory, frame, 65_536, out _).IsNull);
        memory.State.FailAllocation = true;
        Assert.True(ShellFrameResources.AllocateContinuation(ref memory, frame, 1, out _).IsNull);
    }

    [Fact]
    public void Packed_path_commands_resolve_locks_and_commit_atomically()
    {
        var memory = new PathMemory();
        var cli = new APTR(512);
        var names = new APTR(4096);
        memory.State.Names["SYS:C"] = 1;
        memory.State.Names["Work:C"] = 2;
        memory.State.Names["Other:C"] = 3;
        var reset = Encoding.ASCII.GetBytes("SYS:C\0Work:C\0");
        reset.CopyTo(memory.Memory.Bytes, (int)names.Raw);
        Assert.True(ShellCommandPath.TryUpdatePacked(ref memory, cli, names,
            (uint)reset.Length, 2, PathCommand.OperationReset, 1));
        var path = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli);
        Assert.Equal(new[] { 1, 2 }, memory.Groups(path));
        Assert.Equal(1, memory.State.RequesterEnters);
        Assert.Equal(1, memory.State.RequesterExits);

        var add = Encoding.ASCII.GetBytes("SYS:C\0Other:C\0");
        add.CopyTo(memory.Memory.Bytes, (int)names.Raw);
        Assert.True(ShellCommandPath.TryUpdatePacked(ref memory, cli, names,
            (uint)add.Length, 2, PathCommand.OperationAdd, 0));
        path = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli);
        Assert.Equal(new[] { 1, 2, 3 }, memory.Groups(path));

        var remove = Encoding.ASCII.GetBytes("Work:C\0");
        remove.CopyTo(memory.Memory.Bytes, (int)names.Raw);
        Assert.True(ShellCommandPath.TryUpdatePacked(ref memory, cli, names,
            (uint)remove.Length, 1, PathCommand.OperationRemove, 0));
        path = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli);
        Assert.Equal(new[] { 1, 3 }, memory.Groups(path));
        Assert.False(ShellCommandPath.TryUpdatePacked(ref memory, cli, names,
            (uint)remove.Length, 1, PathCommand.OperationRemove, 0));
        Assert.Equal((int)DOS.Error.ObjectNotFound, memory.State.PathError);
        Assert.Equal(path, DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli));
        var absent = Encoding.ASCII.GetBytes("SYS:C\0Missing:C\0");
        absent.CopyTo(memory.Memory.Bytes, (int)names.Raw);
        Assert.False(ShellCommandPath.TryUpdatePacked(ref memory, cli, names,
            (uint)absent.Length, 2, PathCommand.OperationReset, 1));
        Assert.Equal((int)DOS.Error.ObjectNotFound, memory.State.PathError);
        Assert.Equal(path, DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli));
        Assert.Equal(new[] { 1, 3 }, memory.Groups(path));
        Assert.Equal(memory.State.RequesterEnters, memory.State.RequesterExits);
    }

    [Fact]
    public void Runner_owns_optional_buffers_until_their_frame_state_is_inactive()
    {
        var memory = new PathMemory();
        var runner = new APTR(256);
        var frame = new APTR(512);
        var prompt = APTR.Null;
        uint captureLength = 12;
        Assert.True(ShellRunnerBuffers.EnsurePrompt(ref memory, runner, ref prompt));
        var ownedPrompt = prompt;
        Assert.True(ShellRunnerBuffers.EnsurePrompt(ref memory, runner, ref prompt));
        Assert.Equal(ownedPrompt, prompt);
        Assert.True(ShellRunnerBuffers.ReleaseInactivePrompt(ref memory, runner, ref prompt,
            ref captureLength, BPTR.Null, ShellScriptFrameFlags.Interactive,
            ShellScriptPromptExpansionPhase.None, ShellScriptDeferredCommandKind.None));
        Assert.Equal(ownedPrompt, prompt);
        Assert.True(ShellRunnerBuffers.ReleaseInactivePrompt(ref memory, runner, ref prompt,
            ref captureLength, BPTR.Null, ShellScriptFrameFlags.None,
            ShellScriptPromptExpansionPhase.WaitingCommand, ShellScriptDeferredCommandKind.None));
        Assert.Equal(ownedPrompt, prompt);
        Assert.True(ShellRunnerBuffers.ReleaseInactivePrompt(ref memory, runner, ref prompt,
            ref captureLength, BPTR.Null, ShellScriptFrameFlags.None,
            ShellScriptPromptExpansionPhase.None, ShellScriptDeferredCommandKind.None));
        Assert.True(prompt.IsNull);
        Assert.Equal(0u, captureLength);
        Assert.Contains((ownedPrompt.Raw, ShellRunnerBuffers.PromptBytes), memory.State.ReleasedMemory);

        var key = APTR.Null;
        Assert.True(ShellRunnerBuffers.EnsureKey(ref memory, runner, frame, ref key));
        var ownedKey = key;
        Assert.Equal(key.Raw, memory.ReadUInt32(frame, 104));
        Assert.True(ShellRunnerBuffers.ReleaseInactiveKey(ref memory, runner, frame, ref key,
            0, 1, key, 0));
        Assert.Equal(ownedKey, key);
        Assert.True(ShellRunnerBuffers.ReleaseInactiveKey(ref memory, runner, frame, ref key,
            0, 2, key, 0));
        Assert.True(key.IsNull);
        Assert.Equal(0u, memory.ReadUInt32(frame, 104));
        Assert.Contains((ownedKey.Raw, ShellRunnerBuffers.KeyBytes), memory.State.ReleasedMemory);

        var results = APTR.Null;
        uint resultBytes = 0;
        Assert.True(ShellRunnerBuffers.EnsureKeyResults(ref memory, runner, ref results, ref resultBytes));
        Assert.Equal(ShellRunnerBuffers.KeyResultsBytes, resultBytes);
        Assert.Equal(resultBytes, memory.ReadUInt32(runner, 136));
        var snapshot = (byte[])memory.Memory.Bytes.Clone();
        var incorrect = new APTR(results.Raw + 4);
        Assert.False(ShellRunnerBuffers.EnsureKeyResults(ref memory, runner, ref incorrect, ref resultBytes));
        Assert.Equal(snapshot, memory.Memory.Bytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Shell_runner_arena_owns_exactly_its_record_and_optional_workspace(bool packed)
    {
        var memory = new PathMemory();
        Array.Fill(memory.Memory.Bytes, (byte)0xaa, 16384, 22000);
        Assert.True(ShellRunnerArena.TryAllocate(ref memory, packed, out var runner, out var workspace));
        Assert.Equal(16384u, runner.Raw);
        var size = ShellRunnerBuffers.RecordSize + (packed ? ShellScriptWorkspaceLayout.Bytes : 0);
        Assert.All(memory.Memory.Bytes.Skip((int)runner.Raw).Take((int)size), b => Assert.Equal(0, b));
        Assert.Equal(packed ? runner.Raw + ShellRunnerBuffers.RecordSize : 0u, workspace.Raw);
        ShellRunnerArena.Free(ref memory, runner, packed);
        Assert.Contains((runner.Raw, size), memory.State.ReleasedMemory);
        memory.State.FailAllocation = true;
        Assert.False(ShellRunnerArena.TryAllocate(ref memory, packed, out runner, out workspace));
        Assert.True(runner.IsNull && workspace.IsNull);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Runner_cleanup_closes_owned_handles_and_releases_its_allocations_once(bool packed)
    {
        var memory = new PathMemory();
        var runner = new APTR(4096);
        var start = runner.Raw + ShellRunnerBuffers.RecordSize;
        memory.WriteUInt32(runner, 0, 0x5352554e);
        memory.WriteUInt32(runner, 4, 9);
        memory.WriteUInt32(runner, 12, 40000);
        memory.WriteUInt32(runner, 16, 11);
        memory.WriteUInt32(runner, 88, 1);
        memory.WriteUInt32(runner, 128, 12);
        var offsets = new[] { 0u, ShellScriptWorkspaceLayout.Name, ShellScriptWorkspaceLayout.Token,
            ShellScriptWorkspaceLayout.First, ShellScriptWorkspaceLayout.Second, ShellScriptWorkspaceLayout.Third,
            ShellScriptWorkspaceLayout.Fourth, ShellScriptWorkspaceLayout.Redirection, ShellScriptWorkspaceLayout.Input,
            ShellScriptWorkspaceLayout.Output, ShellScriptWorkspaceLayout.Error, ShellScriptWorkspaceLayout.Alias,
            ShellScriptWorkspaceLayout.Lookup, ShellScriptWorkspaceLayout.Temporary };
        var fields = new[] { 20, 24, 28, 32, 36, 40, 44, 52, 56, 60, 64, 68, 72, 108 };
        for (var i = 0; i < fields.Length; i++)
            memory.WriteUInt32(runner, fields[i], packed ? start + offsets[i] : 43000u + (uint)i * 16);
        var temp = memory.ReadUInt32(runner, 108);
        memory.WriteUInt32(runner, 112, 5);
        memory.WriteUInt32(runner, 132, 7);
        memory.WriteUInt32(runner, 48, 50000);
        memory.WriteUInt32(runner, 136, 1024);
        memory.WriteUInt32(runner, 92, 51000);
        memory.WriteUInt32(runner, 96, 12);
        memory.WriteUInt32(runner, 100, 52000);
        memory.WriteUInt32(runner, 116, 53000);
        memory.WriteUInt32(runner, 120, 6);
        memory.WriteUInt32(runner, 124, 54000);

        if (packed)
        {
            memory.WriteUInt32(runner, 24, start + 1);
            Assert.False(ShellRunnerCleanup.CanRelease(ref memory, runner, true));
            Assert.False(ShellRunnerCleanup.Release(ref memory, runner, true));
            Assert.Empty(memory.State.CleanupEvents);
            memory.WriteUInt32(runner, 24, start + ShellScriptWorkspaceLayout.Name);
        }
        Assert.True(ShellRunnerCleanup.Release(ref memory, runner, packed));
        Assert.Equal(new[] { "close:11", "close:12", $"delete:{temp}", $"delete:{temp + 256}" },
            memory.State.CleanupEvents.Take(4));
        Assert.Contains((40000u, ShellScriptFrameCodec.Size), memory.State.ReleasedMemory);
        Assert.Contains((50000u, 1024u), memory.State.ReleasedMemory);
        Assert.Contains((51000u, 12u), memory.State.ReleasedMemory);
        Assert.Contains((52000u, ShellRunnerBuffers.KeyBytes), memory.State.ReleasedMemory);
        Assert.Contains((53000u, 7u), memory.State.ReleasedMemory);
        Assert.Contains((54000u, ShellRunnerBuffers.PromptBytes), memory.State.ReleasedMemory);
        if (packed)
            Assert.DoesNotContain(memory.State.ReleasedMemory, item => item.Item1 >= start && item.Item1 < start + ShellScriptWorkspaceLayout.Bytes);
        else
            Assert.Contains((temp, ShellScriptWorkspaceLayout.SmallBytes), memory.State.ReleasedMemory);
        var released = memory.State.ReleasedMemory.Count;
        Assert.False(ShellRunnerCleanup.Release(ref memory, runner, packed));
        Assert.Equal(released, memory.State.ReleasedMemory.Count);
    }

    [Fact]
    public void Runner_cleanup_does_not_close_a_borrowed_input()
    {
        var memory = new PathMemory();
        var runner = new APTR(4096);
        memory.WriteUInt32(runner, 0, 0x5352554e);
        memory.WriteUInt32(runner, 4, 9);
        memory.WriteUInt32(runner, 16, 11);
        Assert.True(ShellRunnerCleanup.Release(ref memory, runner, false));
        Assert.DoesNotContain("close:11", memory.State.CleanupEvents);
    }

    [Fact]
    public void Shell_cli_fields_use_public_layout_without_modifying_other_fields()
    {
        var memory = new Memory(2048);
        var cli = new APTR(256);
        DosCommandLineInterfaceCodec.Write(ref memory, cli, new CommandLineInterface
        {
            DefaultStack = 1024, FailLevel = 7, ReturnCode = 5,
            CommandDirectory = BPTR.FromRaw(31),
        });
        Assert.True(ShellCliState.TryReadDefaultStack(ref memory, cli, out var bytes));
        Assert.Equal(4096, bytes);
        Assert.True(ShellCliState.TryWriteDefaultStack(ref memory, cli, 8195));
        Assert.True(ShellCliState.TryReadDefaultStack(ref memory, cli, out bytes));
        Assert.Equal(8192, bytes);
        Assert.True(ShellCliState.TryReadFailureLimit(ref memory, cli, out var limit));
        Assert.Equal(7u, limit);
        Assert.True(ShellCliState.TryWriteFailureLimit(ref memory, cli, 0));
        Assert.True(ShellCliState.TryReadFailureLimit(ref memory, cli, out limit));
        Assert.Equal(ShellScriptFrameCodec.DefaultFailureLimit, limit);
        var state = DosCommandLineInterfaceCodec.Read(ref memory, cli);
        Assert.Equal(5, state.ReturnCode);
        Assert.Equal(31u, state.CommandDirectory.Raw);
        var before = (byte[])memory.Bytes.Clone();
        Assert.False(ShellCliState.TryWriteDefaultStack(ref memory, cli, -1));
        Assert.False(ShellCliState.TryWriteFailureLimit(ref memory, cli, uint.MaxValue));
        Assert.False(ShellCliState.TryReadFailureLimit(ref memory, APTR.Null, out _));
        Assert.False(ShellCliState.TryReadDefaultStack(ref memory, new APTR(2046), out _));
        Assert.Equal(before, memory.Bytes);
    }

    [Fact]
    public void Shell_clears_cli_command_file_without_overwriting_its_storage_or_other_fields()
    {
        var memory = new Memory(2048);
        var cli = new APTR(256);
        var name = new APTR(512);
        DosCommandLineInterfaceCodec.Write(ref memory, cli, new CommandLineInterface
        {
            CommandFile = BPTR.FromAddress(name), ReturnCode = 5,
        });
        memory.WriteUInt8(name, 0, 3);
        memory.WriteUInt8(name, 1, (byte)'a');
        memory.WriteUInt8(name, 2, (byte)'b');
        memory.WriteUInt8(name, 3, (byte)'c');
        Assert.True(ShellCliState.TryClearCommandFile(ref memory, cli));
        Assert.Equal(0, memory.ReadUInt8(name));
        Assert.Equal((byte)'a', memory.ReadUInt8(name, 1));
        Assert.Equal(5, DosCommandLineInterfaceCodec.Read(ref memory, cli).ReturnCode);
        DosCommandLineInterfaceCodec.WriteCommandFile(ref memory, cli, BPTR.Null);
        var before = (byte[])memory.Bytes.Clone();
        Assert.False(ShellCliState.TryClearCommandFile(ref memory, cli));
        Assert.False(ShellCliState.TryClearCommandFile(ref memory, APTR.Null));
        Assert.Equal(before, memory.Bytes);
    }

    [Fact]
    public void Shell_lists_public_process_variables_with_kickstart_columns_and_checks_links()
    {
        var memory = new PathMemory();
        var task = new APTR(512); var cli = new APTR(1024);
        var first = new APTR(2048); var second = new APTR(2080);
        var aliasName = new APTR(3000); var aliasValue = new APTR(3040);
        var variableName = new APTR(3100); var variableValue = new APTR(3140);
        Encoding.ASCII.GetBytes("Alias\0").CopyTo(memory.Memory.Bytes, (int)aliasName.Raw);
        Encoding.ASCII.GetBytes("Value").CopyTo(memory.Memory.Bytes, (int)aliasValue.Raw);
        Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRST\0").CopyTo(memory.Memory.Bytes, (int)variableName.Raw);
        Encoding.ASCII.GetBytes("X").CopyTo(memory.Memory.Bytes, (int)variableValue.Raw);
        memory.State.CurrentTask = task;
        DosProcessCodec.WriteCommandLineInterface(ref memory, task, BPTR.FromAddress(cli));
        var list = DosProcessCodec.LocalVariablesAddress(task);
        memory.WriteUInt32(list, 0, first.Raw); memory.WriteUInt32(list, 4, 0); memory.WriteUInt32(list, 8, second.Raw);
        DosLocalVarCodec.Write(ref memory, first, new LocalVar
        {
            Node = new Node { Predecessor = list, Successor = second, Type = (byte)LocalVariableType.Alias,
                Name = STRPTR.FromPointer(aliasName.Raw) }, Value = aliasValue, Length = 5,
        });
        DosLocalVarCodec.Write(ref memory, second, new LocalVar
        {
            Node = new Node { Predecessor = first, Successor = new APTR(list.Raw + 4), Type = (byte)LocalVariableType.Variable,
                Name = STRPTR.FromPointer(variableName.Raw) }, Value = variableValue, Length = 1,
        });
        Assert.True(ShellLocalVariableList.Write(ref memory, cli, new BPTR(1), true));
        Assert.Equal("Alias             Value\n", Encoding.ASCII.GetString(memory.State.ListOutput.ToArray()));
        memory.State.ListOutput.Clear();
        Assert.True(ShellLocalVariableList.Write(ref memory, cli, new BPTR(1), false));
        Assert.Equal("ABCDEFGHIJKLMNOPQRST X\n", Encoding.ASCII.GetString(memory.State.ListOutput.ToArray()));
        memory.WriteUInt32(second, 4, 0);
        Assert.False(ShellLocalVariableList.Write(ref memory, cli, new BPTR(1), false));
        Assert.Equal(DOS.Error.ObjectWrongType, memory.State.ListError);
    }

    [Fact]
    public void Shell_runner_registry_publishes_finds_and_removes_without_dos_state()
    {
        var memory = new Memory(2048);
        var registry = new APTR(64);
        var first = new APTR(128); var middle = new APTR(160); var last = new APTR(192);
        Assert.True(ShellRunnerRegistry.Initialize(ref memory, registry));
        Assert.False(ShellRunnerRegistry.Initialize(ref memory, registry));
        Assert.True(ShellRunnerRegistry.TryPublish(ref memory, registry, first,
            new APTR(512), new APTR(768), new APTR(1024)));
        Assert.False(ShellRunnerRegistry.TryDestroy(ref memory, registry));
        Assert.True(ShellRunnerRegistry.TryPublish(ref memory, registry, middle,
            new APTR(516), new APTR(772), new APTR(1028)));
        Assert.True(ShellRunnerRegistry.TryPublish(ref memory, registry, last,
            new APTR(520), new APTR(776), new APTR(1032)));
        Assert.True(ShellRunnerRegistry.TryFind(ref memory, registry, new APTR(516),
            out var entry, out var frame, out var runner));
        Assert.Equal(middle.Raw, entry.Raw);
        Assert.Equal(772u, frame.Raw);
        Assert.Equal(1028u, runner.Raw);
        var snapshot = (byte[])memory.Bytes.Clone();
        Assert.False(ShellRunnerRegistry.TryPublish(ref memory, registry, new APTR(224),
            new APTR(516), new APTR(800), new APTR(1100)));
        Assert.False(ShellRunnerRegistry.TryPublish(ref memory, registry, new APTR(224),
            new APTR(524), new APTR(772), new APTR(1100)));
        Assert.False(ShellRunnerRegistry.TryPublish(ref memory, registry, new APTR(164),
            new APTR(524), new APTR(800), new APTR(1100)));
        Assert.Equal(snapshot, memory.Bytes);
        Assert.True(ShellRunnerRegistry.TryRemove(ref memory, registry, middle));
        Assert.False(ShellRunnerRegistry.TryFind(ref memory, registry, new APTR(516), out _, out _, out _));
        Assert.True(ShellRunnerRegistry.TryFind(ref memory, registry, new APTR(512), out _, out _, out _));
        Assert.False(ShellRunnerRegistry.TryRemove(ref memory, registry, middle));
        Assert.True(ShellRunnerRegistry.TryRemove(ref memory, registry, last));
        Assert.True(ShellRunnerRegistry.TryRemove(ref memory, registry, first));
        Assert.True(ShellRunnerRegistry.TryDestroy(ref memory, registry));
        Assert.False(ShellRunnerRegistry.TryDestroy(ref memory, registry));
        Assert.True(ShellRunnerRegistry.Initialize(ref memory, registry));
    }

    [Fact]
    public void Shell_process_state_survives_nested_runners_and_restores_the_previous_value()
    {
        var memory = new PathMemory();
        var task = new APTR(512); var cli = new APTR(1024);
        var outer = new APTR(2048); var inner = new APTR(3072); var runner = new APTR(4096);
        var originalName = new APTR(6144); var source = new APTR(6200);
        memory.State.CurrentTask = task;
        DosProcessCodec.WriteCommandLineInterface(ref memory, task, BPTR.FromAddress(cli));
        DosProcessCodec.WriteShellPrivate(ref memory, task, 0x53590004);
        DosCommandLineInterfaceCodec.WriteCommandFile(ref memory, cli, BPTR.FromAddress(originalName));
        memory.WriteUInt8(originalName, 0, 3);
        memory.WriteUInt8(originalName, 1, (byte)'O');
        memory.WriteUInt8(originalName, 2, (byte)'L');
        memory.WriteUInt8(originalName, 3, (byte)'D');
        memory.WriteUInt8(source, 0, (byte)'N');
        memory.WriteUInt8(source, 1, (byte)'E');
        memory.WriteUInt8(source, 2, (byte)'W');
        Assert.True(ShellProcessState.TryGetCommand(ref memory, cli, 5, out var present, out _));
        Assert.False(present);
        Assert.True(ShellProcessState.TryInstall(ref memory, cli, out var state));
        var leases = new ShellStreamLeases { Input = new APTR(7000), Output = new APTR(7100), Error = new APTR(7100) };
        Assert.True(ShellProcessState.TrySetStreamLeases(ref memory, cli, state, in leases));
        var ownedName = DosCommandLineInterfaceCodec.Read(ref memory, cli).CommandFile.Address;
        Assert.NotEqual(originalName, ownedName);
        Assert.Equal(3, memory.ReadUInt8(ownedName));
        Assert.Equal((byte)'O', memory.ReadUInt8(ownedName, 1));
        Assert.True(ShellProcessState.TrySetCommandFile(ref memory, cli, source, 3, out var nameOwned));
        Assert.True(nameOwned);
        Assert.Equal((byte)'N', memory.ReadUInt8(ownedName, 1));
        Assert.False(ShellProcessState.TrySetCommandFile(ref memory, cli, source, 256, out _));
        Assert.False(ShellProcessState.TryInstall(ref memory, cli, out _, out var failure));
        Assert.Equal(ShellProcessStateFailure.AlreadyInstalled, failure);
        Assert.True(ShellProcessState.TryGetCommand(ref memory, cli, 5, out present, out var enabled));
        Assert.True(present); Assert.Equal(1u, enabled);
        Assert.True(ShellProcessState.TrySetCommand(ref memory, cli, 5, 0, out present));
        Assert.True(present);
        Assert.True(ShellRunnerBinding.TryPublish(ref memory, cli, outer, runner));
        Assert.True(ShellRunnerBinding.TryPublishFrame(ref memory, cli, inner, out _));
        Assert.True(ShellProcessState.TryGetStreamLeases(ref memory, cli, out var nestedLeases));
        Assert.Equal(leases.Input, nestedLeases.Input);
        Assert.Equal(leases.Output, nestedLeases.Output);
        Assert.Equal(leases.Error, nestedLeases.Error);
        Assert.True(ShellProcessState.TryEndRequested(ref memory, cli, out var endRequested));
        Assert.False(endRequested);
        Assert.True(ShellProcessState.TryRequestEnd(ref memory, cli));
        Assert.True(ShellProcessState.TryEndRequested(ref memory, cli, out endRequested));
        Assert.True(endRequested);
        Assert.True(ShellProcessState.TryGetCommand(ref memory, cli, 5, out present, out enabled));
        Assert.True(present); Assert.Equal(0u, enabled);
        Assert.False(ShellProcessState.TryRemove(ref memory, cli, state));
        var nested = APTR.FromPointer(DosProcessCodec.ReadShellPrivate(ref memory, task));
        var previous = memory.ReadUInt32(nested, 24);
        memory.WriteUInt32(nested, 24, nested.Raw);
        Assert.False(ShellProcessState.TryGetCommand(ref memory, cli, 5, out _, out _));
        memory.WriteUInt32(nested, 24, previous);
        Assert.False(ShellProcessState.TrySetCommand(ref memory, cli, 0, 1, out _));
        Assert.False(ShellProcessState.TrySetCommand(ref memory, cli, 5, 2, out _));
        Assert.True(ShellRunnerBinding.TryRemoveFrame(ref memory, cli, inner));
        Assert.True(ShellRunnerBinding.TryRemove(ref memory, cli, outer, runner));
        Assert.True(ShellProcessState.TryGetCommand(ref memory, cli, 5, out present, out enabled));
        Assert.True(present); Assert.Equal(0u, enabled);
        Assert.True(ShellProcessState.TrySetCommand(ref memory, cli, 5, 1, out _));
        Assert.True(ShellProcessState.TryGetCommand(ref memory, cli, 5, out _, out enabled));
        Assert.Equal(1u, enabled);
        DosCommandLineInterfaceCodec.WriteCommandFile(ref memory, cli, BPTR.FromAddress(source));
        Assert.False(ShellProcessState.TrySetCommandFile(ref memory, cli, source, 3, out _));
        Assert.False(ShellProcessState.TryRemove(ref memory, cli, state));
        Assert.Equal(state.Raw, DosProcessCodec.ReadShellPrivate(ref memory, task));
        Assert.DoesNotContain((state.Raw, ShellProcessState.Size), memory.State.ReleasedMemory);
        DosCommandLineInterfaceCodec.WriteCommandFile(ref memory, cli, BPTR.FromAddress(ownedName));
        Assert.True(ShellProcessState.TryRemove(ref memory, cli, state));
        Assert.Equal(0x53590004u, DosProcessCodec.ReadShellPrivate(ref memory, task));
        Assert.Equal(BPTR.FromAddress(originalName), DosCommandLineInterfaceCodec.Read(ref memory, cli).CommandFile);
        Assert.Equal((byte)'O', memory.ReadUInt8(originalName, 1));
        Assert.Contains((state.Raw, ShellProcessState.Size), memory.State.ReleasedMemory);
        Assert.False(ShellProcessState.TryRemove(ref memory, cli, state));
        memory.State.FailAllocation = true;
        Assert.False(ShellProcessState.TryInstall(ref memory, cli, out _, out failure));
        Assert.Equal(ShellProcessStateFailure.NoMemory, failure);
        Assert.Equal(0x53590004u, DosProcessCodec.ReadShellPrivate(ref memory, task));
    }

    [Fact]
    public void Shell_process_state_cannot_retire_with_attached_continuations()
    {
        var memory = new PathMemory();
        var task = new APTR(512); var cli = new APTR(1024);
        memory.State.CurrentTask = task;
        DosProcessCodec.WriteCommandLineInterface(ref memory, task, BPTR.FromAddress(cli));
        Assert.True(ShellProcessState.TryGetContinuationHead(ref memory, cli, out var present, out var head));
        Assert.False(present);
        Assert.True(head.IsNull);
        Assert.False(ShellProcessState.TrySetContinuationHead(ref memory, cli, new APTR(2048)));
        Assert.True(ShellProcessState.TryInstall(ref memory, cli, out var state));
        var currentVersion = memory.ReadUInt32(state, 4);
        memory.WriteUInt32(state, 4, 1); // The older state has no continuation-head field.
        Assert.False(ShellProcessState.TryGetContinuationHead(ref memory, cli, out _, out _));
        Assert.False(ShellProcessState.TryRemove(ref memory, cli, state));
        memory.WriteUInt32(state, 4, currentVersion);
        Assert.True(ShellProcessState.TrySetContinuationHead(ref memory, cli, new APTR(2048)));
        Assert.True(ShellProcessState.TryGetContinuationHead(ref memory, cli, out present, out head));
        Assert.True(present);
        Assert.Equal(new APTR(2048), head);
        Assert.False(ShellProcessState.TryRemove(ref memory, cli, state));
        Assert.Equal(state.Raw, DosProcessCodec.ReadShellPrivate(ref memory, task));
        Assert.DoesNotContain((state.Raw, ShellProcessState.Size), memory.State.ReleasedMemory);
        Assert.True(ShellProcessState.TrySetContinuationHead(ref memory, cli, APTR.Null));
        Assert.True(ShellProcessState.TryRemove(ref memory, cli, state));
        Assert.Contains((state.Raw, ShellProcessState.Size), memory.State.ReleasedMemory);
    }

    [Fact]
    public void Shell_runner_binding_is_process_local_and_restores_startup_value()
    {
        var memory = new PathMemory();
        var task = new APTR(512); var cli = new APTR(1024);
        var frame = new APTR(2048); var runner = new APTR(4096);
        memory.State.CurrentTask = task;
        DosProcessCodec.WriteCommandLineInterface(ref memory, task, BPTR.FromAddress(cli));
        DosProcessCodec.WriteShellPrivate(ref memory, task, 0x53590004);
        Assert.True(ShellRunnerBinding.TryFind(ref memory, cli, out var absent, out _, out _));
        Assert.True(absent.IsNull);
        Assert.True(ShellRunnerBinding.TryPublish(ref memory, cli, frame, runner));
        Assert.True(ShellRunnerBinding.TryFind(ref memory, cli, out var binding, out var foundFrame, out var foundRunner));
        Assert.Equal(frame, foundFrame); Assert.Equal(runner, foundRunner);
        Assert.False(ShellRunnerBinding.TryPublish(ref memory, cli, frame, runner, out var failure));
        Assert.Equal(ShellRunnerBindingFailure.AlreadyBound, failure);
        Assert.False(ShellRunnerBinding.TryRemove(ref memory, cli, frame, new APTR(4100)));
        Assert.Equal(binding.Raw, DosProcessCodec.ReadShellPrivate(ref memory, task));
        memory.WriteUInt32(binding, 4, 99);
        Assert.False(ShellRunnerBinding.TryFind(ref memory, cli, out _, out _, out _));
        Assert.False(ShellRunnerBinding.TryRemove(ref memory, cli, frame, runner));
        memory.WriteUInt32(binding, 4, 1);
        var otherTask = new APTR(1280);
        DosProcessCodec.WriteCommandLineInterface(ref memory, otherTask, BPTR.FromAddress(cli));
        DosProcessCodec.WriteShellPrivate(ref memory, otherTask, binding.Raw);
        memory.State.CurrentTask = otherTask;
        Assert.False(ShellRunnerBinding.TryFind(ref memory, cli, out _, out _, out _));
        memory.State.CurrentTask = task;
        Assert.True(ShellRunnerBinding.TryRemove(ref memory, cli, frame, runner));
        Assert.Equal(0x53590004u, DosProcessCodec.ReadShellPrivate(ref memory, task));
        Assert.Contains((binding.Raw, ShellRunnerBinding.Size), memory.State.ReleasedMemory);
        memory.State.FailAllocation = true;
        Assert.False(ShellRunnerBinding.TryPublish(ref memory, cli, frame, runner, out failure));
        Assert.Equal(ShellRunnerBindingFailure.NoMemory, failure);
        Assert.Equal(0x53590004u, DosProcessCodec.ReadShellPrivate(ref memory, task));
    }

    [Fact]
    public void Frame_bindings_nest_and_restore_the_runner_and_startup_marker()
    {
        var memory = new PathMemory();
        var task = new APTR(512); var cli = new APTR(1024);
        var outer = new APTR(2048); var inner = new APTR(3072); var runner = new APTR(4096);
        memory.State.CurrentTask = task;
        DosProcessCodec.WriteCommandLineInterface(ref memory, task, BPTR.FromAddress(cli));
        DosProcessCodec.WriteShellPrivate(ref memory, task, 0x53590004);
        Assert.True(ShellRunnerBinding.TryPublish(ref memory, cli, outer, runner));
        Assert.True(ShellRunnerBinding.TryFind(ref memory, cli, out var owner, out _, out _));
        Assert.True(ShellRunnerBinding.TryPublishFrame(ref memory, cli, outer, out _));
        Assert.Equal(owner.Raw, DosProcessCodec.ReadShellPrivate(ref memory, task));
        Assert.True(ShellRunnerBinding.TryPublishFrame(ref memory, cli, inner, out _));
        Assert.True(ShellRunnerBinding.TryFind(ref memory, cli, out var nested, out var frame, out var foundRunner));
        Assert.NotEqual(owner, nested);
        Assert.Equal(inner, frame);
        Assert.True(foundRunner.IsNull);
        Assert.False(ShellRunnerBinding.TryRemoveFrame(ref memory, cli, outer));
        Assert.True(ShellRunnerBinding.TryRemoveFrame(ref memory, cli, inner));
        Assert.Equal(owner.Raw, DosProcessCodec.ReadShellPrivate(ref memory, task));
        Assert.False(ShellRunnerBinding.TryRemoveFrame(ref memory, cli, outer));
        Assert.True(ShellRunnerBinding.TryRemove(ref memory, cli, outer, runner));
        Assert.Equal(0x53590004u, DosProcessCodec.ReadShellPrivate(ref memory, task));
        Assert.True(ShellRunnerBinding.TryPublishFrame(ref memory, cli, outer, out _));
        Assert.True(ShellRunnerBinding.TryRemoveFrame(ref memory, cli, outer));
        Assert.Equal(0x53590004u, DosProcessCodec.ReadShellPrivate(ref memory, task));
        memory.State.FailAllocation = true;
        Assert.False(ShellRunnerBinding.TryPublishFrame(ref memory, cli, inner, out var failure));
        Assert.Equal(ShellRunnerBindingFailure.NoMemory, failure);
        Assert.Equal(0x53590004u, DosProcessCodec.ReadShellPrivate(ref memory, task));
    }

    [Fact]
    public void Command_path_updates_transfer_cli_ownership_atomically()
    {
        var memory = new PathMemory();
        var cli = new APTR(512);
        var a = memory.AddLock(1); var b = memory.AddLock(2); var c = memory.AddLock(3);
        var old = memory.AddNode(a, BPTR.Null);
        var requested = memory.AddNode(b, BPTR.Null);
        DosCommandLineInterfaceCodec.WriteCommandDirectory(ref memory, cli, old);

        Assert.True(ShellCommandPath.TryUpdate(ref memory, cli, requested, PathCommand.OperationAdd));
        var updated = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli);
        Assert.Equal(new[] { 1, 2 }, memory.Groups(updated));
        Assert.Contains(a.Raw, memory.State.ReleasedLocks);
        Assert.Contains(old.Address.Raw, memory.State.FreedNodes);
        Assert.DoesNotContain(b.Raw, memory.State.ReleasedLocks);

        var remove = memory.AddNode(memory.AddLock(1), BPTR.Null);
        Assert.True(ShellCommandPath.TryUpdate(ref memory, cli, remove, PathCommand.OperationRemove));
        updated = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli);
        Assert.Equal(new[] { 2 }, memory.Groups(updated));
        var replacement = memory.AddNode(c, BPTR.Null);
        Assert.True(ShellCommandPath.TryUpdate(ref memory, cli, replacement, PathCommand.OperationReset));
        updated = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli);
        Assert.Equal(new[] { 3 }, memory.Groups(updated));

        var before = (byte[])memory.Memory.Bytes.Clone();
        Assert.False(ShellCommandPath.TryUpdate(ref memory, cli, remove, PathCommand.OperationRemove));
        Assert.Equal(before, memory.Memory.Bytes);
        memory.State.FailAllocation = true;
        Assert.False(ShellCommandPath.TryUpdate(ref memory, cli, requested, PathCommand.OperationAdd));
        Assert.Equal(updated, DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli));
        Assert.Equal(new[] { 3 }, memory.Groups(updated));
        Assert.DoesNotContain(c.Raw, memory.State.ReleasedLocks);
        memory.State.FailAllocation = false;
        var freedBefore = memory.State.FreedNodes.Count;
        memory.State.FailOnDuplicateCall = memory.State.DuplicateCalls + 2;
        Assert.False(ShellCommandPath.TryUpdate(ref memory, cli, requested, PathCommand.OperationAdd));
        Assert.Equal(updated, DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli));
        Assert.Equal(new[] { 3 }, memory.Groups(updated));
        Assert.Equal(freedBefore + 1, memory.State.FreedNodes.Count);
        Assert.False(ShellCommandPath.TryUpdate(ref memory, cli, updated, PathCommand.OperationAdd));
        var sharedLockNode = memory.AddNode(DosPathLockCodec.Read(ref memory, updated.Address).Lock, BPTR.Null);
        Assert.False(ShellCommandPath.TryUpdate(ref memory, cli, sharedLockNode, PathCommand.OperationReset));
    }

    [Fact]
    public void Command_path_rejects_a_cycle_before_modifying_or_releasing_it()
    {
        var memory = new PathMemory();
        var cli = new APTR(512);
        var node = memory.AddNode(memory.AddLock(1), BPTR.Null);
        memory.WriteUInt32(node.Address, 0, node.Raw);
        DosCommandLineInterfaceCodec.WriteCommandDirectory(ref memory, cli, node);
        var before = (byte[])memory.Memory.Bytes.Clone();
        Assert.False(ShellCommandPath.TryUpdate(ref memory, cli, BPTR.Null, PathCommand.OperationReset));
        Assert.Equal(before, memory.Memory.Bytes);
        Assert.Empty(memory.State.FreedNodes);
        Assert.Empty(memory.State.ReleasedLocks);
    }

    [Fact]
    public void Command_path_can_extend_and_release_more_than_256_owned_entries()
    {
        var memory = new PathMemory();
        var cli = new APTR(512);
        var original = BPTR.Null;
        for (var number = 300; number >= 1; number--)
            original = memory.AddNode(memory.AddLock(number), original);
        DosCommandLineInterfaceCodec.WriteCommandDirectory(ref memory, cli, original);
        var requested = memory.AddNode(memory.AddLock(301), BPTR.Null);

        Assert.True(ShellCommandPath.TryUpdate(ref memory, cli, requested, PathCommand.OperationAdd));
        var updated = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli);
        Assert.Equal(Enumerable.Range(1, 301), memory.Groups(updated));
        Assert.Equal(300, memory.State.FreedNodes.Count);
        Assert.Equal(300, memory.State.ReleasedLocks.Count);
        Assert.True(ShellCommandPath.TryUpdate(ref memory, cli, BPTR.Null, PathCommand.OperationReset));
        Assert.True(DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli).IsNull);
        Assert.Equal(601, memory.State.FreedNodes.Count);
        Assert.Equal(601, memory.State.ReleasedLocks.Count);
    }

    [Fact]
    public void Command_path_packed_update_accepts_more_than_64_directories()
    {
        var memory = new PathMemory();
        var cli = new APTR(512);
        var text = new APTR(12000);
        var position = 0;
        for (var number = 0; number < 65; number++)
        {
            var name = $"D{number}";
            memory.State.Names.Add(name, number + 1);
            foreach (var character in name)
                memory.WriteUInt8(text, position++, (byte)character);
            memory.WriteUInt8(text, position++, 0);
        }
        Assert.True(ShellCommandPath.TryUpdatePacked(ref memory, cli, text,
            (uint)position, 65, PathCommand.OperationReset, 0));
        var head = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli);
        Assert.Equal(Enumerable.Range(1, 65), memory.Groups(head));
        Assert.True(ShellCommandPath.TryUpdatePacked(ref memory, cli, APTR.Null,
            0, 0, PathCommand.OperationReset, 0));
        Assert.True(DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli).IsNull);
    }

    [Fact]
    public void Command_path_cursor_detects_a_cycle_after_a_noncyclic_prefix()
    {
        var memory = new PathMemory();
        var third = memory.AddNode(memory.AddLock(3), BPTR.Null);
        var second = memory.AddNode(memory.AddLock(2), third);
        var first = memory.AddNode(memory.AddLock(1), second);
        memory.WriteUInt32(third.Address, 0, second.Raw);
        var cursor = new ShellCommandPathCursor(first);
        Assert.True(ShellCommandPath.TryAdvance(ref memory, ref cursor, out var firstLock));
        Assert.Equal(1, memory.State.Locks[firstLock.Raw]);
        Assert.False(ShellCommandPath.TryAdvance(ref memory, ref cursor, out _));

        var cli = new APTR(512);
        DosCommandLineInterfaceCodec.WriteCommandDirectory(ref memory, cli, first);
        Assert.False(ShellCommandPath.TryUpdate(ref memory, cli, BPTR.Null, PathCommand.OperationReset));
        Assert.Empty(memory.State.FreedNodes);
        Assert.Empty(memory.State.ReleasedLocks);
    }

    [Fact]
    public void Command_path_reset_deduplicates_requested_locks_and_can_clear_the_list()
    {
        var memory = new PathMemory();
        var cli = new APTR(512);
        Assert.False(ShellCommandPath.TryUpdate(ref memory, APTR.Null, BPTR.Null, PathCommand.OperationReset));
        var second = memory.AddNode(memory.AddLock(9), BPTR.Null);
        var first = memory.AddNode(memory.AddLock(9), second);
        Assert.True(ShellCommandPath.TryUpdate(ref memory, cli, first, PathCommand.OperationReset));
        var path = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli);
        Assert.Equal(new[] { 9 }, memory.Groups(path));
        Assert.Empty(memory.State.ReleasedLocks);
        Assert.True(ShellCommandPath.TryUpdate(ref memory, cli, BPTR.Null, PathCommand.OperationReset));
        Assert.True(DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli).IsNull);
        Assert.Contains(path.Address.Raw, memory.State.FreedNodes);
    }

    [Fact]
    public void Shell_runner_registry_rejects_corrupt_lists_and_foreign_entries_without_mutation()
    {
        var memory = new Memory(2048);
        var registry = new APTR(64); var other = new APTR(96); var entry = new APTR(128);
        Assert.True(ShellRunnerRegistry.Initialize(ref memory, registry));
        Assert.True(ShellRunnerRegistry.Initialize(ref memory, other));
        Assert.True(ShellRunnerRegistry.TryPublish(ref memory, registry, entry,
            new APTR(512), new APTR(768), new APTR(1024)));
        Assert.False(ShellRunnerRegistry.TryPublish(ref memory, other, entry,
            new APTR(516), new APTR(772), new APTR(1028)));
        memory.WriteUInt32(entry, 8, entry.Raw); // cycle
        var snapshot = (byte[])memory.Bytes.Clone();
        Assert.False(ShellRunnerRegistry.TryFind(ref memory, registry, new APTR(512), out _, out _, out _));
        Assert.False(ShellRunnerRegistry.TryRemove(ref memory, registry, entry));
        Assert.False(ShellRunnerRegistry.TryPublish(ref memory, registry, new APTR(160),
            new APTR(516), new APTR(772), new APTR(1028)));
        Assert.Equal(snapshot, memory.Bytes);
        Assert.False(ShellRunnerRegistry.Initialize(ref memory, new APTR(2044)));
        Assert.False(ShellRunnerRegistry.Initialize(ref memory, new APTR(65)));
    }

    [Fact]
    public void Script_workspace_parts_are_contiguous_and_fit_the_native_arena()
    {
        Assert.Equal(ShellScriptWorkspaceLayout.LineBytes, ShellScriptWorkspaceLayout.Name);
        Assert.Equal(ShellScriptWorkspaceLayout.Name + ShellScriptWorkspaceLayout.NameBytes, ShellScriptWorkspaceLayout.Token);
        Assert.Equal(ShellScriptWorkspaceLayout.Token + ShellScriptWorkspaceLayout.TokenBytes, ShellScriptWorkspaceLayout.First);
        Assert.Equal(ShellScriptWorkspaceLayout.Fourth + ShellScriptWorkspaceLayout.SmallBytes, ShellScriptWorkspaceLayout.Redirection);
        Assert.Equal(ShellScriptWorkspaceLayout.Redirection + ShellScriptWorkspaceLayout.LineBytes, ShellScriptWorkspaceLayout.Input);
        Assert.Equal(ShellScriptWorkspaceLayout.Alias + ShellScriptWorkspaceLayout.LineBytes, ShellScriptWorkspaceLayout.Lookup);
        Assert.Equal(ShellScriptWorkspaceLayout.Temporary + ShellScriptWorkspaceLayout.SmallBytes, ShellScriptWorkspaceLayout.Bytes);
        Assert.Equal(21248u, ShellScriptWorkspaceLayout.Bytes);
    }

    [Fact]
    public void Command_path_nodes_use_public_guest_layout_and_reject_invalid_pointers()
    {
        var memory = new Memory(2048);
        var address = new APTR(256);
        memory.WriteUInt32(address, 0, 100);
        memory.WriteUInt32(address, 4, 37);
        Assert.True(ShellCommandPath.TryReadNode(ref memory, BPTR.FromAddress(address),
            out var next, out var directory));
        Assert.Equal(100u, next.Raw);
        Assert.Equal(37u, directory.Raw);
        Assert.False(ShellCommandPath.TryReadNode(ref memory, BPTR.Null, out _, out _));
        Assert.False(ShellCommandPath.TryReadNode(ref memory, BPTR.FromRaw(0x40000040), out _, out _));
        Assert.False(ShellCommandPath.TryReadNode(ref memory, BPTR.FromRaw(511), out _, out _));
    }

    [Fact]
    public void Script_frame_can_be_owned_by_a_memory_only_guest()
    {
        var memory = new Memory(2048);
        var frame = new APTR(256);
        var initial = new ShellScriptFrameState
        {
            Cli = new APTR(32), Input = new BPTR(2), Output = new BPTR(3),
            Flags = ShellScriptFrameFlags.Active, FailureLimit = 10,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref memory, frame, in initial));
        Assert.True(ShellScriptFrameCodec.TryRecordResult(ref memory, frame, 5));
        Assert.True(ShellScriptFrameCodec.TrySetCurrentDirectory(ref memory, frame, new BPTR(37)));
        Assert.True(ShellScriptFrameCodec.TryRead(ref memory, frame, out var actual));
        Assert.Equal(initial.Cli, actual.Cli);
        Assert.Equal(initial.Input, actual.Input);
        Assert.Equal(5, actual.LastResult);
        Assert.Equal(37u, actual.CurrentDirectory.Raw);
        Assert.Equal(ShellScriptFrameFlags.Active, actual.Flags);
        Assert.False(ShellScriptFrameCodec.TrySetCurrentDirectory(ref memory, new APTR(1), new BPTR(38)));
    }

    [Fact]
    public void Parsers_accept_memory_only_platform_and_preserve_quoted_text()
    {
        var memory = new Memory(2048);
        var source = new APTR(16);
        const string line = "echo \"hello > world\" >>\"DH0:result file\"";
        Encoding.ASCII.GetBytes(line).CopyTo(memory.Bytes, 16);
        var workspace = new ShellRedirectionWorkspace(new APTR(256), 256,
            new APTR(512), 256, new APTR(768), 256, new APTR(1024), 256);
        Assert.True(ShellRedirectionParser.Parse(ref memory, source, (uint)line.Length,
            in workspace, out var spec, out var length));
        Assert.Equal(1u, spec.OutputAppend);
        Assert.Equal("DH0:result file", Encoding.ASCII.GetString(memory.Bytes, 768, (int)spec.OutputLength));
        var cursor = new ShellTextCursor(workspace.Command, length);
        Assert.Equal((int)ShellTextTokenResult.Token, ShellTextParser.NextToken(ref memory,
            ref cursor, new APTR(1300), 256, out _, out _));
        Assert.Equal((int)ShellTextTokenResult.Token, ShellTextParser.NextToken(ref memory,
            ref cursor, new APTR(1300), 256, out var tokenLength, out var flags));
        Assert.Equal("hello > world", Encoding.ASCII.GetString(memory.Bytes, 1300, (int)tokenLength));
        Assert.Equal(ShellTextParser.TokenUsedSyntax, flags);
    }

    private sealed class PathState
    {
        public uint NextNode = 1024;
        public uint NextLock = 100;
        public uint NextRunnerMemory = 16384;
        public APTR CurrentTask;
        public bool FailAllocation;
        public int DuplicateCalls;
        public int FailOnDuplicateCall;
        public readonly Dictionary<uint, int> Locks = new();
        public readonly Dictionary<string, int> Names = new(StringComparer.OrdinalIgnoreCase);
        public int PathError;
        public int RequesterEnters;
        public int RequesterExits;
        public int LockFailure;
        public readonly List<uint> ReleasedLocks = new();
        public readonly List<uint> FreedNodes = new();
        public readonly List<(uint, uint)> ReleasedMemory = new();
        public readonly List<string> CleanupEvents = new();
        public readonly List<byte> ListOutput = new();
        public DOS.Error ListError;
    }

    private struct PathMemory : IShellPackedCommandPathPlatform, IShellRunnerCleanupPlatform, IShellRunnerBindingPlatform,
        IShellLocalVariableListPlatform
    {
        public Memory Memory;
        public PathState State;
        public PathMemory() { Memory = new Memory(65536); State = new PathState(); }
        public APTR AllocateRunnerMemory(uint size)
        {
            if (State.FailAllocation) return APTR.Null;
            var value = new APTR(State.NextRunnerMemory); State.NextRunnerMemory = (State.NextRunnerMemory + size + 3) & ~3u; return value;
        }
        public void FreeRunnerMemory(APTR address, uint size) { State.CleanupEvents.Add($"free:{address.Raw}"); State.ReleasedMemory.Add((address.Raw, size)); }
        public APTR CurrentRunnerProcess() => State.CurrentTask;
        public APTR CurrentVariableProcess() => State.CurrentTask;
        public void SetVariableListError(DOS.Error error) => State.ListError = error;
        public int Write(BPTR handle, APTR source, uint length)
        {
            for (var i = 0u; i < length; i++) State.ListOutput.Add(ReadUInt8(source, (int)i));
            return (int)length;
        }
        public bool IsRunnerBindingMemory(APTR address, uint size) => address.IsNotNull && IsMapped(address, size);
        public void CloseRunnerInput(BPTR input) => State.CleanupEvents.Add($"close:{input.Raw}");
        public void DeleteRunnerPath(APTR path) => State.CleanupEvents.Add($"delete:{path.Raw}");
        public bool TryEnterPathRequesters(uint quiet, out APTR process, out APTR previousWindow)
        {
            State.RequesterEnters++; process = new APTR(100); previousWindow = new APTR(104); return true;
        }
        public void ExitPathRequesters(APTR process, APTR previousWindow) => State.RequesterExits++;
        public BPTR LockPathDirectory(APTR name)
        {
            if (State.LockFailure != 0) { State.PathError = State.LockFailure; return BPTR.Null; }
            var length = 0;
            while (ReadUInt8(name, length) != 0) length++;
            var path = Encoding.ASCII.GetString(Memory.Bytes, (int)name.Raw, length);
            if (!State.Names.TryGetValue(path, out var group))
            { State.PathError = (int)DOS.Error.ObjectNotFound; return BPTR.Null; }
            State.PathError = 0;
            return AddLock(group);
        }
        public int GetPathError() => State.PathError;
        public void SetPathError(int error) => State.PathError = error;
        public BPTR AddLock(int group) { var result = BPTR.FromRaw(State.NextLock++); State.Locks[result.Raw] = group; return result; }
        public BPTR AddNode(BPTR directory, BPTR next)
        {
            var node = AllocatePathNode();
            DosPathLockCodec.Write(ref this, node, new PathLock { Next = next, Lock = directory });
            return BPTR.FromAddress(node);
        }
        public int[] Groups(BPTR node)
        {
            var values = new List<int>();
            while (node.IsNotNull)
            {
                Assert.True(ShellCommandPath.TryReadNode(ref this, node, out var next, out var directory));
                values.Add(State.Locks[directory.Raw]); node = next;
            }
            return values.ToArray();
        }
        public APTR AllocatePathNode() { if (State.FailAllocation) return APTR.Null; var node = new APTR(State.NextNode); State.NextNode += 8; return node; }
        public void FreePathNode(APTR node) => State.FreedNodes.Add(node.Raw);
        public BPTR DuplicatePathLock(BPTR directory)
        {
            State.DuplicateCalls++;
            return State.DuplicateCalls == State.FailOnDuplicateCall
                ? BPTR.Null : AddLock(State.Locks[directory.Raw]);
        }
        public void ReleasePathLock(BPTR directory) => State.ReleasedLocks.Add(directory.Raw);
        public bool TrySamePathLock(BPTR first, BPTR second, out bool same) { same = State.Locks[first.Raw] == State.Locks[second.Raw]; return true; }
        public bool IsMapped(APTR address, uint count) => Memory.IsMapped(address, count);
        public byte ReadUInt8(APTR address, int offset = 0) => Memory.ReadUInt8(address, offset);
        public ushort ReadUInt16(APTR address, int offset = 0) => Memory.ReadUInt16(address, offset);
        public uint ReadUInt32(APTR address, int offset = 0) => Memory.ReadUInt32(address, offset);
        public void WriteUInt8(APTR address, int offset, byte value) => Memory.WriteUInt8(address, offset, value);
        public void WriteUInt16(APTR address, int offset, ushort value) => Memory.WriteUInt16(address, offset, value);
        public void WriteUInt32(APTR address, int offset, uint value) => Memory.WriteUInt32(address, offset, value);
        public void Clear(APTR address, uint count) => Memory.Clear(address, count);
        public void Copy(APTR source, APTR destination, uint count) => Memory.Copy(source, destination, count);
    }

    private struct Memory(int size) : IAmigaGuestMemory
    {
        public readonly byte[] Bytes = new byte[size];
        public bool IsMapped(APTR address, uint count) => address.Raw <= Bytes.Length && count <= (uint)Bytes.Length - address.Raw;
        public byte ReadUInt8(APTR address, int offset = 0) => Bytes[checked((int)address.Raw + offset)];
        public ushort ReadUInt16(APTR address, int offset = 0) => (ushort)((ReadUInt8(address, offset) << 8) | ReadUInt8(address, offset + 1));
        public uint ReadUInt32(APTR address, int offset = 0) => ((uint)ReadUInt16(address, offset) << 16) | ReadUInt16(address, offset + 2);
        public void WriteUInt8(APTR address, int offset, byte value) => Bytes[checked((int)address.Raw + offset)] = value;
        public void WriteUInt16(APTR address, int offset, ushort value) { WriteUInt8(address, offset, (byte)(value >> 8)); WriteUInt8(address, offset + 1, (byte)value); }
        public void WriteUInt32(APTR address, int offset, uint value) { WriteUInt16(address, offset, (ushort)(value >> 16)); WriteUInt16(address, offset + 2, (ushort)value); }
        public void Clear(APTR address, uint count) => Array.Clear(Bytes, (int)address.Raw, (int)count);
        public void Copy(APTR source, APTR destination, uint count) => Array.Copy(Bytes, (int)source.Raw, Bytes, (int)destination.Raw, (int)count);
    }
}
