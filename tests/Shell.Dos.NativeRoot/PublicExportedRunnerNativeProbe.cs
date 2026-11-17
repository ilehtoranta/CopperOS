using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Calls the exported script entries with absent and irrelevant private DOS contexts.</summary>
public static class PublicExportedRunnerNativeProbe
{
    private const uint MessageBytes = 24;
    private const uint TagBytes = 40;
    private const uint ContextBytes = DosShellNativeContextCore.ExecBaseOffset + 4;

    [M68kEntryPoint]
    [M68kExport("copperos.shell.public-exported-runner-probe")]
    public static int Main()
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        var script = Text("SYS:T/exported-script");
        var output = Text("SYS:T/exported-output");
        var scriptText = Text("Echo >SYS:T/exported-output PASS\n");
        _ = PublicShellFileServices.Delete(dos, output);
        var file = PublicShellFileServices.OpenOutput(dos, script, Length(script), 0);
        var ok = file.IsNotNull;
        if (file.IsNotNull)
        {
            ok = PublicShellFileServices.Write(dos, file, scriptText, Length(scriptText)) == (int)Length(scriptText);
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        var reply = global::Amiga.Exec.CreateMsgPort();
        var message = Allocate(MessageBytes);
        var tags = Allocate(TagBytes);
        ok = ok && reply.IsNotNull && message.IsNotNull && tags.IsNotNull;
        if (ok)
        {
            APTR.WriteUInt32(message, ExecLayout.Message.ReplyPort, reply.Raw);
            APTR.WriteUInt16(message, ExecLayout.Message.Length, (ushort)MessageBytes);
            Tag(tags, 0, DosNewProcessTag.Entry, APTR.ExportAddress("copperos.shell.public-exported-runner-worker").Raw);
            Tag(tags, 1, DosNewProcessTag.Cli, 1);
            Tag(tags, 2, DosNewProcessTag.StackSize, 65_536);
            Tag(tags, 3, DosNewProcessTag.Name, Text("CopperOS Exported Runner Probe").Raw);
            var child = ChildProbeDosVectors.CreateNewProc(dos, tags);
            if (child.IsNull) ok = false;
            else
            {
                global::Amiga.Exec.PutMsg(DosProcessCodec.MessagePortAddress(child), message);
                _ = global::Amiga.Exec.WaitPort(reply);
                ok = global::Amiga.Exec.GetMsg(reply) == message && APTR.ReadUInt32(message, 20) == 1;
                // ReplyMsg precedes DOS process teardown. The worker still
                // executes code in this HUNK until it has left Exec's task
                // lists; a guest-side timeout cannot safely unload that code.
                // The outer emulator harness bounds a stuck test instead.
                while (global::Amiga.Exec.FindTask((CString)"CopperOS Exported Runner Probe").IsNotNull)
                    ChildProbeDosVectors.Delay(dos, 1);
            }
        }
        if (tags.IsNotNull) global::Amiga.Exec.FreeMem(tags.Raw, TagBytes);
        if (message.IsNotNull) global::Amiga.Exec.FreeMem(message.Raw, MessageBytes);
        if (reply.IsNotNull) global::Amiga.Exec.DeleteMsgPort(reply);
        var buffer = Allocate(32);
        file = PublicShellFileServices.OpenInput(dos, output, Length(output));
        if (buffer.IsNull || file.IsNull) ok = false;
        else
        {
            ok = PublicShellFileServices.ReadLine(dos, file, buffer, 32, out var received, out var eof) &&
                eof == 0 && received == 4 && APTR.ReadUInt32(buffer, 0) == 0x50415353u && ok;
        }
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        if (buffer.IsNotNull) global::Amiga.Exec.FreeMem(buffer.Raw, 32);
        _ = PublicShellFileServices.Delete(dos, script);
        _ = PublicShellFileServices.Delete(dos, output);
        var marker = Text(ok ? (CString)"SYS:runner-ok" : (CString)"SYS:runner-fail");
        file = PublicShellFileServices.OpenOutput(dos, marker, Length(marker), 0);
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    [M68kExport("copperos.shell.public-exported-runner-worker")]
    public static uint Worker()
    {
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var port = DosProcessCodec.MessagePortAddress(task);
        _ = global::Amiga.Exec.WaitPort(port);
        var message = global::Amiga.Exec.GetMsg(port);
        if (message.IsNull) return 20;
        var shell = new DosShellNativePlatform(APTR.Null);
        var cli = DosProcessCodec.ReadCommandLineInterface(ref shell, task).Address;
        var originalPrivate = DosProcessCodec.ReadShellPrivate(ref shell, task);
        var originalCommandFile = DosCommandLineInterfaceCodec.Read(ref shell, cli).CommandFile;
        var canary = Allocate(ContextBytes);
        if (canary.IsNotNull)
            for (var i = 0u; i < ContextBytes; i++) APTR.WriteUInt8(canary, (int)i, 0xa5);
        var installed = ShellProcessState.TryInstall(ref shell, cli, out var state);
        var ok = installed && canary.IsNotNull;
        if (installed)
        {
            var script = Text("SYS:T/exported-script");
            var exec = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
            var execBefore = APTR.ReadUInt32(APTR.FromPointer(4), 0);
            for (var context = 0; context < 3; context++)
            {
                // Address 4 contains ExecBase, not a CopperStart DOS state.
                // A public runner must neither initialize nor dereference it
                // as a private context, even when the ABI argument is nonzero.
                var privateContext = context == 0 ? APTR.Null : context == 1 ? canary : APTR.FromPointer(4);
                var pair = ExportedRunnerCalls.Begin(APTR.ExportAddress("copperos.shell.execute-begin"),
                    cli.Raw, script.Raw, Length(script), privateContext, exec);
                var result = M68kRuntime.SplitUInt64(pair, out var status);
                ok = status == (uint)ShellScriptExecutionStatus.Completed && result == 0 && ok;
                pair = ExportedRunnerCalls.Poll(APTR.ExportAddress("copperos.shell.execute-poll"),
                    cli.Raw, privateContext, exec);
                result = M68kRuntime.SplitUInt64(pair, out status);
                ok = status == (uint)ShellScriptExecutionStatus.Failed &&
                    result == (uint)ShellCommandResult.Error &&
                    APTR.ReadUInt32(APTR.FromPointer(4), 0) == execBefore && ok;
            }
            for (var i = 0u; canary.IsNotNull && i < ContextBytes; i++)
                ok = APTR.ReadUInt8(canary, (int)i) == 0xa5 && ok;
            ok = ShellProcessState.TryRemove(ref shell, cli, state) &&
                DosProcessCodec.ReadShellPrivate(ref shell, task) == originalPrivate &&
                DosCommandLineInterfaceCodec.Read(ref shell, cli).CommandFile == originalCommandFile && ok;
        }
        if (canary.IsNotNull) global::Amiga.Exec.FreeMem(canary.Raw, ContextBytes);
        APTR.WriteUInt32(message, 20, ok ? 1u : 0u);
        global::Amiga.Exec.ReplyMsg(message);
        return ok ? 0u : 10u;
    }

    private static APTR Allocate(uint bytes) => APTR.FromPointer(global::Amiga.Exec.AllocMem(bytes,
        global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
    private static APTR Text(CString text) => APTR.FromPointer(CString.ToUInt32(text));
    private static uint Length(APTR text)
    { uint length = 0; while (APTR.ReadUInt8(text, (int)length) != 0) length++; return length; }
    private static void Tag(APTR tags, int index, DosNewProcessTag tag, uint value)
    { APTR.WriteUInt32(tags, index * 8, (uint)tag); APTR.WriteUInt32(tags, index * 8 + 4, value); }
}

internal static class ExportedRunnerCalls
{
    [AmigaIndirectCall(M68kRegister.A3)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern ulong Begin([M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.D0)] uint cli, [M68kRegister(M68kRegister.D1)] uint file,
        [M68kRegister(M68kRegister.D2)] uint length, [M68kRegister(M68kRegister.A0)] APTR state,
        [M68kRegister(M68kRegister.A6)] APTR exec);
    [AmigaIndirectCall(M68kRegister.A3)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern ulong Poll([M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.D0)] uint cli, [M68kRegister(M68kRegister.A0)] APTR state,
        [M68kRegister(M68kRegister.A6)] APTR exec);
}
