using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Checks runner directory state after two real CD commands.</summary>
public static class PublicRunnerDirectoryNativeProbe
{
    [M68kEntryPoint]
    [M68kExport("copperos.shell.runner-directory-probe")]
    public static int Main()
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        var script = Text("SYS:T/directory-script");
        var first = Text("SYS:T/directory-first");
        var second = Text("SYS:T/directory-second");
        _ = PublicShellFileServices.Delete(dos, first);
        _ = PublicShellFileServices.Delete(dos, second);
        var fixture = Text("SYS:fixture");
        var lockValue = PublicShellFileServices.Lock(dos, fixture);
        var fromDf1 = lockValue.IsNull;
        if (fromDf1) lockValue = PublicShellFileServices.Lock(dos, Text("DF1:fixture"));
        var ok = lockValue.IsNotNull;
        if (lockValue.IsNotNull) PublicShellFileServices.UnLock(dos, lockValue);
        var scriptText = fromDf1
            ? Text("CD SYS:T\nDF1:fixture >SYS:T/directory-first\nCD SYS:S\nDF1:fixture >SYS:T/directory-second\n")
            : Text("CD SYS:T\nSYS:fixture >SYS:T/directory-first\nCD SYS:S\nSYS:fixture >SYS:T/directory-second\n");
        var file = ok ? PublicShellFileServices.OpenOutput(dos, script, Length(script), 0) : BPTR.Null;
        if (file.IsNull) ok = false;
        else
        {
            ok = PublicShellFileServices.Write(dos, file, scriptText, Length(scriptText)) == (int)Length(scriptText);
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        var reply = global::Amiga.Exec.CreateMsgPort();
        var message = Allocate(24);
        var tags = Allocate(40);
        ok = ok && reply.IsNotNull && message.IsNotNull && tags.IsNotNull;
        if (ok)
        {
            APTR.WriteUInt32(message, ExecLayout.Message.ReplyPort, reply.Raw);
            APTR.WriteUInt16(message, ExecLayout.Message.Length, 24);
            Tag(tags, 0, DosNewProcessTag.Entry, APTR.ExportAddress("copperos.shell.runner-directory-worker").Raw);
            Tag(tags, 1, DosNewProcessTag.Cli, 1);
            Tag(tags, 2, DosNewProcessTag.StackSize, 65_536);
            Tag(tags, 3, DosNewProcessTag.Name, Text("CopperOS Directory Runner Probe").Raw);
            var child = ChildProbeDosVectors.CreateNewProc(dos, tags);
            if (child.IsNull) ok = false;
            else
            {
                global::Amiga.Exec.PutMsg(DosProcessCodec.MessagePortAddress(child), message);
                _ = global::Amiga.Exec.WaitPort(reply);
                ok = global::Amiga.Exec.GetMsg(reply) == message && APTR.ReadUInt32(message, 20) == 1;
                while (global::Amiga.Exec.FindTask((CString)"CopperOS Directory Runner Probe").IsNotNull)
                    ChildProbeDosVectors.Delay(dos, 1);
            }
        }
        if (tags.IsNotNull) global::Amiga.Exec.FreeMem(tags.Raw, 40);
        if (message.IsNotNull) global::Amiga.Exec.FreeMem(message.Raw, 24);
        if (reply.IsNotNull) global::Amiga.Exec.DeleteMsgPort(reply);
        ok = IsPass(dos, first) && IsPass(dos, second) && ok;
        _ = PublicShellFileServices.Delete(dos, script);
        _ = PublicShellFileServices.Delete(dos, first);
        _ = PublicShellFileServices.Delete(dos, second);
        var marker = Text(ok ? (CString)"SYS:runner-ok" : (CString)"SYS:runner-fail");
        file = PublicShellFileServices.OpenOutput(dos, marker, Length(marker), 0);
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    [M68kExport("copperos.shell.runner-directory-worker")]
    public static uint Worker()
    {
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var port = DosProcessCodec.MessagePortAddress(task);
        _ = global::Amiga.Exec.WaitPort(port);
        var message = global::Amiga.Exec.GetMsg(port);
        if (message.IsNull) return 20;
        var shell = new DosShellNativePlatform(APTR.Null);
        var cli = DosProcessCodec.ReadCommandLineInterface(ref shell, task).Address;
        var ok = ShellProcessState.TryInstall(ref shell, cli, out var state);
        if (ok)
        {
            var script = Text("SYS:T/directory-script");
            ok = shell.TryExecuteScript(cli, script, Length(script), APTR.Null, 0, out var result) ==
                ShellScriptExecutionStatus.Completed && result == 0;
            ok = ShellProcessState.TryRemove(ref shell, cli, state) && ok;
        }
        APTR.WriteUInt32(message, 20, ok ? 1u : 0u);
        global::Amiga.Exec.ReplyMsg(message);
        return ok ? 0u : 10u;
    }

    private static bool IsPass(APTR dos, APTR path)
    {
        var buffer = Allocate(16);
        var file = PublicShellFileServices.OpenInput(dos, path, Length(path));
        var ok = buffer.IsNotNull && file.IsNotNull &&
            PublicShellFileServices.ReadLine(dos, file, buffer, 16, out var length, out var eof) &&
            eof == 0 && length == 4 && APTR.ReadUInt32(buffer, 0) == 0x50415353;
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        if (buffer.IsNotNull) global::Amiga.Exec.FreeMem(buffer.Raw, 16);
        return ok;
    }
    private static APTR Allocate(uint size) => APTR.FromPointer(global::Amiga.Exec.AllocMem(size,
        global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
    private static uint Length(APTR text)
    { uint length = 0; while (APTR.ReadUInt8(text, (int)length) != 0) length++; return length; }
    private static void Tag(APTR tags, int index, DosNewProcessTag tag, uint value)
    { APTR.WriteUInt32(tags, index * 8, (uint)tag); APTR.WriteUInt32(tags, index * 8 + 4, value); }
}
