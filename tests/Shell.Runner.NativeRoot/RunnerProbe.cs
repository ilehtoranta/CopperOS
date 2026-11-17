using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Shell.Runner.NativeRoot;

/// <summary>Guest probe for the shared CLI fields and optional runner buffers.</summary>
public static class RunnerProbe
{
    [M68kEntryPoint]
    public static int Main()
    {
        var exec = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
        var dos = Vectors.OpenLibrary(exec, Text("dos.library"), 37);
        if (dos.IsNull) return 20;
        var result = Run();
        if (!RunFileServices(dos)) result = 10;
        var name = result == 0 ? Text("DH0:runner-ok") : Text("DH0:runner-fail");
        var file = Vectors.Open(dos, name, 1006);
        if (file.IsNotNull) Vectors.Close(dos, file);
        ExecVectors.CloseLibrary(exec, dos);
        return result;
    }

    private static bool RunFileServices(APTR dos)
    {
        var name = Text("DH0:io-test");
        var data = Text("AB");
        var memory = new Guest();
        var buffer = memory.AllocateRunnerMemory(8);
        if (buffer.IsNull) return false;
        var file = PublicShellFileServices.OpenOutput(dos, name, 11, 0);
        var ok = file.IsNotNull;
        if (file.IsNotNull)
        {
            ok = PublicShellFileServices.Write(dos, file, data, 2) == 2 &&
                PublicShellFileServices.WriteByte(dos, file, (byte)'C') == (byte)'C';
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        file = PublicShellFileServices.OpenOutput(dos, name, 11, 1);
        if (file.IsNull) ok = false;
        else
        {
            ok = PublicShellFileServices.Write(dos, file, Text("D"), 1) == 1 &&
                PublicShellFileServices.WriteByte(dos, file, (byte)'E') == (byte)'E' && ok;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        file = PublicShellFileServices.OpenInput(dos, name, 11);
        if (file.IsNull) ok = false;
        else
        {
            ok = PublicShellFileServices.Read(dos, file, buffer, 8) == 5 &&
                memory.ReadUInt32(buffer) == 0x41424344 && memory.ReadUInt8(buffer, 4) == (byte)'E' &&
                PublicShellFileServices.Read(dos, file, buffer, 8) == 0 && ok;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        var invalid = PublicShellFileServices.OpenInput(dos, name, 1);
        var pathPlatform = new PublicShellPathPlatform(dos);
        ok = invalid.IsNull && pathPlatform.GetPathError() == (int)DOS.Error.BadStreamName && ok;
        if (invalid.IsNotNull) PublicShellFileServices.Close(dos, invalid);
        ok = PublicShellFileServices.Delete(dos, name) != 0 && ok;
        file = PublicShellFileServices.OpenOutput(dos, name, 11, 1);
        if (file.IsNull) ok = false;
        else
        {
            ok = PublicShellFileServices.WriteByte(dos, file, (byte)'Z') == (byte)'Z' && ok;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        file = PublicShellFileServices.OpenInput(dos, name, 11);
        if (file.IsNull) ok = false;
        else
        {
            ok = PublicShellFileServices.Read(dos, file, buffer, 8) == 1 &&
                memory.ReadUInt8(buffer) == (byte)'Z' && ok;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        ok = PublicShellFileServices.Delete(dos, name) != 0 && ok;
        invalid = PublicShellFileServices.OpenOutput(dos, name, 11, 2);
        ok = invalid.IsNull && pathPlatform.GetPathError() == (int)DOS.Error.BadStreamName && ok;
        if (invalid.IsNotNull) PublicShellFileServices.Close(dos, invalid);
        file = PublicShellFileServices.OpenOutput(dos, name, 11, 0);
        if (file.IsNull) ok = false;
        else
        {
            ok = PublicShellFileServices.Write(dos, file, Text("AB\r\nC\nZ"), 7) == 7 && ok;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        file = PublicShellFileServices.OpenInput(dos, name, 11);
        if (file.IsNull) ok = false;
        else
        {
            uint length = 0, eof = 0;
            ok = PublicShellFileServices.IsInteractive(dos, file) == 0 &&
                PublicShellFileServices.ReadLine(dos, file, buffer, 8, out length, out eof) &&
                length == 2 && eof == 0 && memory.ReadUInt8(buffer) == (byte)'A' &&
                memory.ReadUInt8(buffer, 1) == (byte)'B' && memory.ReadUInt8(buffer, 2) == 0 && ok;
            ok = PublicShellFileServices.Seek(dos, file, 0, 0) == 4 && ok;
            ok = PublicShellFileServices.ReadLine(dos, file, buffer, 8, out length, out eof) &&
                length == 1 && eof == 0 && memory.ReadUInt8(buffer) == (byte)'C' && ok;
            ok = PublicShellFileServices.Seek(dos, file, 0, 0) == 6 && ok;
            ok = PublicShellFileServices.ReadLine(dos, file, buffer, 8, out length, out eof) &&
                length == 1 && eof == 0 && memory.ReadUInt8(buffer) == (byte)'Z' && ok;
            ok = PublicShellFileServices.Seek(dos, file, 0, 0) == 7 && ok;
            memory.WriteUInt8(buffer, 0, (byte)'Q');
            ok = PublicShellFileServices.ReadLine(dos, file, buffer, 8, out length, out eof) &&
                length == 0 && eof == 1 && memory.ReadUInt8(buffer) == (byte)'Q' && ok;
            ok = PublicShellFileServices.Seek(dos, file, 0, -1) >= 0 &&
                PublicShellFileServices.ReadLine(dos, file, buffer, 8, out length, out eof) &&
                length == 2 && memory.ReadUInt8(buffer) == (byte)'A' && ok;
            ok = !PublicShellFileServices.ReadLine(dos, file, buffer, 1, out length, out eof) &&
                pathPlatform.GetPathError() == (int)DOS.Error.ObjectWrongType && ok;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        ok = PublicShellFileServices.Delete(dos, name) != 0 && ok;
        memory.FreeRunnerMemory(buffer, 8);
        return ok;
    }

    private static int Run()
    {
        var memory = new Guest();
        var runner = memory.AllocateRunnerMemory(ShellRunnerBuffers.RecordSize);
        var frame = memory.AllocateRunnerMemory(ShellScriptFrameCodec.Size);
        if (runner.IsNull || frame.IsNull) return 1;
        var prompt = APTR.Null;
        uint captureLength = 5;
        var key = APTR.Null;
        var results = APTR.Null;
        uint resultBytes = 0;
        var ok = ShellRunnerBuffers.EnsurePrompt(ref memory, runner, ref prompt) &&
            prompt.IsNotNull &&
            ShellRunnerBuffers.ReleaseInactivePrompt(ref memory, runner, ref prompt,
                ref captureLength, BPTR.Null, ShellScriptFrameFlags.None,
                ShellScriptPromptExpansionPhase.None, ShellScriptDeferredCommandKind.None) &&
            prompt.IsNull && captureLength == 0 &&
            ShellRunnerBuffers.EnsureKey(ref memory, runner, frame, ref key) &&
            key.IsNotNull && memory.ReadUInt32(frame, 104) == key.Raw &&
            ShellRunnerBuffers.ReleaseInactiveKey(ref memory, runner, frame, ref key,
                0, 2, key, 0) && key.IsNull && memory.ReadUInt32(frame, 104) == 0 &&
            ShellRunnerBuffers.EnsureKeyResults(ref memory, runner, ref results, ref resultBytes) &&
            resultBytes == ShellRunnerBuffers.KeyResultsBytes;
        if (results.IsNotNull) memory.FreeRunnerMemory(results, resultBytes);
        memory.FreeRunnerMemory(frame, ShellScriptFrameCodec.Size);
        memory.FreeRunnerMemory(runner, ShellRunnerBuffers.RecordSize);
        ok = RunCleanup(ref memory) && RunBinding(ref memory) && RunFrameResources(ref memory) && ok;
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref memory, task).Address;
        ok = ok && ShellCliState.TryReadDefaultStack(ref memory, cli, out var stack) &&
            ShellCliState.TryWriteDefaultStack(ref memory, cli, stack) &&
            ShellCliState.TryReadFailureLimit(ref memory, cli, out var limit) &&
            limit > 0;
        return ok ? 0 : 10;
    }

    private static bool RunFrameResources(ref Guest memory)
    {
        var frame = memory.AllocateRunnerMemory(ShellScriptFrameCodec.Size);
        if (frame.IsNull) return false;
        var initial = new ShellScriptFrameState { Cli = frame, Flags = ShellScriptFrameFlags.Active };
        if (!ShellScriptFrameCodec.Initialize(ref memory, frame, in initial))
        { memory.FreeRunnerMemory(frame, ShellScriptFrameCodec.Size); return false; }
        var control = ShellFrameResources.AllocateControl(ref memory, frame);
        var nested = ShellFrameResources.AllocateControl(ref memory, frame);
        var label = ShellFrameResources.AllocateLabel(ref memory, frame, 4, out var name);
        if (control.IsNull || nested.IsNull || label.IsNull || name.IsNull)
        {
            if (control.IsNotNull) ShellFrameResources.FreeControl(ref memory, frame, control);
            if (nested.IsNotNull) ShellFrameResources.FreeControl(ref memory, frame, nested);
            if (label.IsNotNull) ShellFrameResources.FreeLabel(ref memory, frame, label);
            memory.FreeRunnerMemory(frame, ShellScriptFrameCodec.Size);
            return false;
        }
        memory.WriteUInt32(control, 0, ShellScriptControlCodec.Magic);
        memory.WriteUInt32(control, 4, ShellScriptControlCodec.Version);
        memory.WriteUInt32(control, 12, (uint)ShellScriptBlockKind.If);
        memory.WriteUInt32(nested, 0, ShellScriptControlCodec.Magic);
        memory.WriteUInt32(nested, 4, ShellScriptControlCodec.Version);
        memory.WriteUInt32(nested, 8, control.Raw);
        memory.WriteUInt32(nested, 12, (uint)ShellScriptBlockKind.If);
        memory.WriteUInt32(frame, 68, nested.Raw);
        memory.WriteUInt32(label, 0, ShellScriptLabelCodec.Magic);
        memory.WriteUInt32(label, 4, ShellScriptLabelCodec.Version);
        memory.WriteUInt32(label, 12, name.Raw);
        memory.WriteUInt32(label, 16, 4);
        memory.WriteUInt32(frame, 64, 1);
        memory.WriteUInt32(frame, 76, label.Raw);
        var ok = ShellFrameResources.CanRelease(ref memory, frame);
        memory.WriteUInt32(label, 8, label.Raw);
        ok = !ShellFrameResources.CanRelease(ref memory, frame) && ok;
        memory.WriteUInt32(label, 8, 0);
        ok = ShellFrameResources.Release(ref memory, frame) &&
            memory.ReadUInt32(frame, 64) == 0 && memory.ReadUInt32(frame, 68) == 0 &&
            memory.ReadUInt32(frame, 76) == 0 && ok;
        memory.FreeRunnerMemory(frame, ShellScriptFrameCodec.Size);
        return ok;
    }

    private static bool RunCleanup(ref Guest memory)
    {
        if (!ShellRunnerArena.TryAllocate(ref memory, true, out var runner, out var workspace)) return false;
        var frame = memory.AllocateRunnerMemory(ShellScriptFrameCodec.Size);
        var optional = memory.AllocateRunnerMemory(ShellRunnerBuffers.KeyResultsBytes);
        if (frame.IsNull || optional.IsNull)
        {
            if (frame.IsNotNull) memory.FreeRunnerMemory(frame, ShellScriptFrameCodec.Size);
            if (optional.IsNotNull) memory.FreeRunnerMemory(optional, ShellRunnerBuffers.KeyResultsBytes);
            ShellRunnerArena.Free(ref memory, runner, true);
            return false;
        }
        var start = workspace.Raw;
        memory.WriteUInt32(runner, 0, 0x5352554e); memory.WriteUInt32(runner, 4, 9);
        memory.WriteUInt32(runner, 8, frame.Raw); memory.WriteUInt32(runner, 12, frame.Raw);
        memory.WriteUInt32(runner, 16, 12); memory.WriteUInt32(runner, 76, (uint)ShellScriptRunnerState.Running);
        memory.WriteUInt32(runner, 20, start);
        memory.WriteUInt32(runner, 24, start + ShellScriptWorkspaceLayout.Name);
        memory.WriteUInt32(runner, 28, start + ShellScriptWorkspaceLayout.Token);
        memory.WriteUInt32(runner, 32, start + ShellScriptWorkspaceLayout.First);
        memory.WriteUInt32(runner, 36, start + ShellScriptWorkspaceLayout.Second);
        memory.WriteUInt32(runner, 40, start + ShellScriptWorkspaceLayout.Third);
        memory.WriteUInt32(runner, 44, start + ShellScriptWorkspaceLayout.Fourth);
        memory.WriteUInt32(runner, 48, optional.Raw); memory.WriteUInt32(runner, 136, ShellRunnerBuffers.KeyResultsBytes);
        memory.WriteUInt32(runner, 52, start + ShellScriptWorkspaceLayout.Redirection);
        memory.WriteUInt32(runner, 56, start + ShellScriptWorkspaceLayout.Input);
        memory.WriteUInt32(runner, 60, start + ShellScriptWorkspaceLayout.Output);
        memory.WriteUInt32(runner, 64, start + ShellScriptWorkspaceLayout.Error);
        memory.WriteUInt32(runner, 68, start + ShellScriptWorkspaceLayout.Alias);
        memory.WriteUInt32(runner, 72, start + ShellScriptWorkspaceLayout.Lookup);
        memory.WriteUInt32(runner, 108, start + ShellScriptWorkspaceLayout.Temporary);
        var codecOk = ShellScriptRunnerCodec.TryRead(ref memory, runner, out var stored) &&
            stored.Frame == frame && stored.ErrorCodes == optional &&
            ShellScriptRunnerCodec.TrySetState(ref memory, runner, ShellScriptRunnerState.Pending, 5, 3) &&
            ShellScriptRunnerCodec.TryRead(ref memory, runner, out stored) &&
            stored.State == ShellScriptRunnerState.Pending && stored.Result == 5 && stored.Steps == 3;
        var cleaned = ShellRunnerCleanup.Release(ref memory, runner, true);
        var ok = codecOk && cleaned && !ShellRunnerCleanup.Release(ref memory, runner, true) &&
            memory.ReadUInt32(runner, 0) == 0;
        if (!cleaned)
        {
            memory.FreeRunnerMemory(frame, ShellScriptFrameCodec.Size);
            memory.FreeRunnerMemory(optional, ShellRunnerBuffers.KeyResultsBytes);
        }
        ShellRunnerArena.Free(ref memory, runner, true);
        return ok;
    }

    private static bool RunBinding(ref Guest memory)
    {
        var process = memory.AllocateRunnerMemory(DosProcessCodec.Size);
        var cli = memory.AllocateRunnerMemory(DosCommandLineInterfaceCodec.Size);
        var frame = memory.AllocateRunnerMemory(ShellScriptFrameCodec.Size);
        var nested = memory.AllocateRunnerMemory(ShellScriptFrameCodec.Size);
        var runner = memory.AllocateRunnerMemory(ShellRunnerBuffers.RecordSize);
        if (process.IsNull || cli.IsNull || frame.IsNull || nested.IsNull || runner.IsNull)
        {
            if (runner.IsNotNull) memory.FreeRunnerMemory(runner, ShellRunnerBuffers.RecordSize);
            if (nested.IsNotNull) memory.FreeRunnerMemory(nested, ShellScriptFrameCodec.Size);
            if (frame.IsNotNull) memory.FreeRunnerMemory(frame, ShellScriptFrameCodec.Size);
            if (cli.IsNotNull) memory.FreeRunnerMemory(cli, DosCommandLineInterfaceCodec.Size);
            if (process.IsNotNull) memory.FreeRunnerMemory(process, DosProcessCodec.Size);
            return false;
        }
        memory.Task = process;
        DosProcessCodec.WriteCommandLineInterface(ref memory, process, BPTR.FromAddress(cli));
        DosProcessCodec.WriteShellPrivate(ref memory, process, 0x53590004);
        var ok = ShellRunnerBinding.TryPublish(ref memory, cli, frame, runner) &&
            ShellRunnerBinding.TryFind(ref memory, cli, out var binding, out var foundFrame, out var foundRunner) &&
            binding.IsNotNull && foundFrame == frame && foundRunner == runner &&
            !ShellRunnerBinding.TryPublish(ref memory, cli, frame, runner) &&
            ShellRunnerBinding.TryPublishFrame(ref memory, cli, nested, out _) &&
            ShellRunnerBinding.TryFind(ref memory, cli, out var inner, out var innerFrame, out var innerRunner) &&
            inner.IsNotNull && innerFrame == nested && innerRunner.IsNull &&
            !ShellRunnerBinding.TryRemoveFrame(ref memory, cli, frame) &&
            ShellRunnerBinding.TryRemoveFrame(ref memory, cli, nested) &&
            ShellRunnerBinding.TryFind(ref memory, cli, out var restored, out _, out var restoredRunner) &&
            restored == binding && restoredRunner == runner &&
            ShellRunnerBinding.TryRemove(ref memory, cli, frame, runner) &&
            DosProcessCodec.ReadShellPrivate(ref memory, process) == 0x53590004;
        memory.WriteUInt32(process, 0, 1);
        ok = !ShellRunnerBinding.TryPublish(ref memory, cli, frame, runner, out var failure) &&
            failure == ShellRunnerBindingFailure.NoMemory &&
            DosProcessCodec.ReadShellPrivate(ref memory, process) == 0x53590004 && ok;
        memory.Task = APTR.Null;
        memory.FreeRunnerMemory(runner, ShellRunnerBuffers.RecordSize);
        memory.FreeRunnerMemory(nested, ShellScriptFrameCodec.Size);
        memory.FreeRunnerMemory(frame, ShellScriptFrameCodec.Size);
        memory.FreeRunnerMemory(cli, DosCommandLineInterfaceCodec.Size);
        memory.FreeRunnerMemory(process, DosProcessCodec.Size);
        return ok;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}

internal struct Guest : IShellRunnerCleanupPlatform, IShellRunnerBindingPlatform
{
    public APTR Task;
    public APTR AllocateRunnerMemory(uint size)
    {
        if (Task.IsNotNull && APTR.ReadUInt32(Task, 0) == 1)
        { APTR.WriteUInt32(Task, 0, 0); return APTR.Null; }
        return APTR.FromPointer(global::Amiga.Exec.AllocMem(size,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
    }
    public void FreeRunnerMemory(APTR address, uint size) => global::Amiga.Exec.FreeMem(address.Raw, size);
    public void CloseRunnerInput(BPTR input) { }
    public void DeleteRunnerPath(APTR path) { }
    public APTR CurrentRunnerProcess() => Task;
    public bool IsRunnerBindingMemory(APTR address, uint size)
    {
        if (address.IsNull || address.Raw > uint.MaxValue - size) return false;
        for (var offset = 0u; offset < size; offset += 4)
            if (global::Amiga.Exec.TypeOfMem(APTR.FromPointer(address.Raw + offset)) == 0) return false;
        return true;
    }
    public bool IsMapped(APTR address, uint size) => address.IsNotNull && address.Raw <= uint.MaxValue - size;
    public byte ReadUInt8(APTR address, int offset = 0) => APTR.ReadUInt8(address, offset);
    public ushort ReadUInt16(APTR address, int offset = 0) => APTR.ReadUInt16(address, offset);
    public uint ReadUInt32(APTR address, int offset = 0) => APTR.ReadUInt32(address, offset);
    public void WriteUInt8(APTR address, int offset, byte value) => APTR.WriteUInt8(address, offset, value);
    public void WriteUInt16(APTR address, int offset, ushort value) => APTR.WriteUInt16(address, offset, value);
    public void WriteUInt32(APTR address, int offset, uint value) => APTR.WriteUInt32(address, offset, value);
    public void Clear(APTR address, uint size) { for (uint i = 0; i < size; i++) WriteUInt8(address, (int)i, 0); }
    public void Copy(APTR source, APTR destination, uint size) { for (uint i = 0; i < size; i++) WriteUInt8(destination, (int)i, ReadUInt8(source, (int)i)); }
}

[AmigaLibrary("exec.library::shell-runner-probe", AmigaLibraryBasePolicy.CallerProvided)]
internal static class ExecVectors
{
    [AmigaLvo(ExecLvo.OpenLibrary)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR OpenLibrary([M68kRegister(M68kRegister.A6)] APTR exec,
        [M68kRegister(M68kRegister.A1)] APTR name, [M68kRegister(M68kRegister.D0)] uint version);
    [AmigaLvo(ExecLvo.CloseLibrary)]
    internal static extern void CloseLibrary([M68kRegister(M68kRegister.A6)] APTR exec, [M68kRegister(M68kRegister.A1)] APTR library);
}

[AmigaLibrary("dos.library::shell-runner-probe", AmigaLibraryBasePolicy.CallerProvided)]
internal static class Vectors
{
    public static APTR OpenLibrary(APTR exec, APTR name, uint version) => ExecVectors.OpenLibrary(exec, name, version);
    [AmigaLvo(DosLvo.Open)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR Open([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name, [M68kRegister(M68kRegister.D2)] int mode);
    [AmigaLvo(DosLvo.Close)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Close([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR file);
}
