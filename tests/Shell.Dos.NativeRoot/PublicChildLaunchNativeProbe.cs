using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Exercises public Process creation, path copying and borrowed output ownership.</summary>
public static class PublicChildLaunchNativeProbe
{
    private const uint MessageBytes = 24;
    private const uint TagBytes = 56;

    [M68kEntryPoint]
    [M68kExport("copperos.shell.public-child-launch-probe")]
    public static int Main()
    {
        var ok = Run();
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        var path = Text(ok ? (CString)"SYS:T/child-ok" : (CString)"SYS:T/child-fail");
        var marker = PublicShellFileServices.OpenOutput(dos, path, Length(path), 0);
        if (marker.IsNotNull) _ = PublicShellFileServices.Close(dos, marker);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    private static bool Run()
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return false;
        var memory = new CopperSharpNativeDosPlatform();
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var parentCli = DosProcessCodec.ReadCommandLineInterface(ref memory, task).Address;
        var parentPath = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, parentCli);
        var name = Text("SYS:T/child-output");
        var output = PublicShellFileServices.OpenOutput(dos, name, Length(name), 0);
        var reply = global::Amiga.Exec.CreateMsgPort();
        var message = Allocate(MessageBytes);
        var tags = Allocate(TagBytes);
        var ok = !parentPath.IsNull && output.IsNotNull && reply.IsNotNull && message.IsNotNull && tags.IsNotNull;
        if (ok)
        {
            APTR.WriteUInt32(message, ExecLayout.Message.ReplyPort, reply.Raw);
            APTR.WriteUInt16(message, ExecLayout.Message.Length, (ushort)MessageBytes);
            Tag(tags, 0, DosNewProcessTag.Entry, APTR.ExportAddress("copperos.shell.public-child-launch-worker").Raw);
            Tag(tags, 1, DosNewProcessTag.Cli, 1);
            Tag(tags, 2, DosNewProcessTag.Output, output.Raw);
            Tag(tags, 3, DosNewProcessTag.CloseOutput, 0);
            Tag(tags, 4, DosNewProcessTag.StackSize, 16_384);
            Tag(tags, 5, DosNewProcessTag.Name, Text("CopperOS Public Child Probe").Raw);
            ok = Write(dos, output, "before\n");
            var child = ok ? ChildProbeDosVectors.CreateNewProc(dos, tags) : APTR.Null;
            if (child.IsNull) ok = false;
            else
            {
                var childCli = DosProcessCodec.ReadCommandLineInterface(ref memory, child).Address;
                var childPath = childCli.IsNull ? BPTR.Null :
                    DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, childCli);
                if (childCli.IsNotNull)
                    ok = CaptureCli(dos, ref memory, child, childCli) && ok;
                ok = childCli.IsNotNull && childPath.IsNotNull && childPath != parentPath &&
                    ShellCommandPath.TryReadNode(ref memory, parentPath, out _, out var parentLock) &&
                    ShellCommandPath.TryReadNode(ref memory, childPath, out _, out var childLock) &&
                    parentLock.IsNotNull && childLock.IsNotNull && parentLock != childLock &&
                    PublicShellPathVectors.SameLock(dos, parentLock, childLock) == 0 && ok;
                var port = APTR.FromPointer(child.Raw + DosLayout.Process.MessagePort);
                global::Amiga.Exec.PutMsg(port, message);
                _ = global::Amiga.Exec.WaitPort(reply);
                var returned = global::Amiga.Exec.GetMsg(reply);
                ok = returned == message && APTR.ReadUInt32(message, 20) == 1 && ok;
                // A reply does not mean that DOS has retired the child or
                // stopped executing this HUNK. Let the emulator harness bound
                // a stuck test instead of unloading code the child still uses.
                while (global::Amiga.Exec.FindTask((CString)"CopperOS Public Child Probe").IsNotNull)
                    ChildProbeDosVectors.Delay(dos, 1);
                ok = Write(dos, output, "after\n") && ok;
            }
        }
        if (tags.IsNotNull) global::Amiga.Exec.FreeMem(tags.Raw, TagBytes);
        if (message.IsNotNull) global::Amiga.Exec.FreeMem(message.Raw, MessageBytes);
        if (reply.IsNotNull) global::Amiga.Exec.DeleteMsgPort(reply);
        if (output.IsNotNull) ok = PublicShellFileServices.Close(dos, output) != 0 && ok;
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    [M68kExport("copperos.shell.public-child-launch-worker")]
    public static uint Worker()
    {
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var port = APTR.FromPointer(task.Raw + DosLayout.Process.MessagePort);
        _ = global::Amiga.Exec.WaitPort(port);
        var message = global::Amiga.Exec.GetMsg(port);
        if (message.IsNull) return 20;
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        var memory = new CopperSharpNativeDosPlatform();
        var cli = DosProcessCodec.ReadCommandLineInterface(ref memory, task).Address;
        var ok = dos.IsNotNull && cli.IsNotNull &&
            DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli).IsNotNull &&
            Write(dos, PublicShellFileServices.Output(dos), "child\n");
        APTR.WriteUInt32(message, 20, ok ? 1u : 0u);
        if (dos.IsNotNull) global::Amiga.Exec.CloseLibrary(dos);
        global::Amiga.Exec.ReplyMsg(message);
        return ok ? 0u : 10u;
    }

    private static APTR Allocate(uint bytes) => APTR.FromPointer(global::Amiga.Exec.AllocMem(bytes,
        global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
    private static APTR Text(CString text) => APTR.FromPointer(CString.ToUInt32(text));
    private static uint Length(APTR text)
    { uint length = 0; while (APTR.ReadUInt8(text, (int)length) != 0) length++; return length; }
    private static bool Write(APTR dos, BPTR output, CString value)
    { var text = Text(value); var length = Length(text); return PublicShellFileServices.Write(dos, output, text, length) == (int)length; }
    private static bool CaptureCli(APTR dos, ref CopperSharpNativeDosPlatform memory, APTR task, APTR cli)
    {
        var buffer = Allocate(32);
        if (buffer.IsNull) return false;
        var value = DosCommandLineInterfaceCodec.Read(ref memory, cli);
        APTR.WriteUInt32(buffer, 0, value.StandardInput.Raw);
        APTR.WriteUInt32(buffer, 4, value.CurrentInput.Raw);
        APTR.WriteUInt32(buffer, 8, DosProcessCodec.ReadCurrentInput(ref memory, task).Raw);
        APTR.WriteUInt32(buffer, 12, value.StandardOutput.Raw);
        APTR.WriteUInt32(buffer, 16, value.CurrentOutput.Raw);
        APTR.WriteUInt32(buffer, 20, DosProcessCodec.ReadCurrentOutput(ref memory, task).Raw);
        APTR.WriteUInt32(buffer, 24, unchecked((uint)value.DefaultStack));
        APTR.WriteUInt32(buffer, 28, DosProcessCodec.ReadCurrentError(ref memory, task).Raw);
        var name = Text("SYS:T/child-cli.bin");
        var file = PublicShellFileServices.OpenOutput(dos, name, Length(name), 0);
        var ok = file.IsNotNull && PublicShellFileServices.Write(dos, file, buffer, 32) == 32;
        if (file.IsNotNull) ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        global::Amiga.Exec.FreeMem(buffer.Raw, 32);
        return ok;
    }
    private static void Tag(APTR tags, int index, DosNewProcessTag tag, uint value)
    { APTR.WriteUInt32(tags, index * 8, (uint)tag); APTR.WriteUInt32(tags, index * 8 + 4, value); }
}

[AmigaLibrary("dos.library::shell-child-launch-probe", AmigaLibraryBasePolicy.CallerProvided)]
internal static class ChildProbeDosVectors
{
    [AmigaLvo(DosLvo.CreateNewProc)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR CreateNewProc([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR tags);
    [AmigaLvo(DosLvo.Delay)]
    internal static extern void Delay([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] int ticks);
}
