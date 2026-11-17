using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Observes public CLI and task visibility during a DOS exit callback.</summary>
public static class PublicExitLifecycleNativeProbe
{
    private const uint MessageBytes = 36;
    private const uint TagBytes = 56;

    [M68kEntryPoint]
    [M68kExport("copperos.shell.exit-lifecycle-probe")]
    public static int Main()
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        var message = Allocate(MessageBytes);
        var tags = Allocate(TagBytes);
        var name = Allocate(PublicShellTaskIdentity.NameBytes);
        var secondName = Allocate(PublicShellTaskIdentity.NameBytes);
        var reservation = APTR.Null;
        var secondReservation = APTR.Null;
        var reply = global::Amiga.Exec.CreateMsgPort();
        var ok = message.IsNotNull && tags.IsNotNull && name.IsNotNull &&
            secondName.IsNotNull && reply.IsNotNull &&
            PublicShellTaskIdentity.TryReserve(message, name, PublicShellTaskIdentity.NameBytes, out reservation) &&
            PublicShellTaskIdentity.TryReserve(message, secondName, PublicShellTaskIdentity.NameBytes, out secondReservation);
        if (ok)
        {
            var different = false;
            for (var i = 0; i < (int)PublicShellTaskIdentity.NameBytes; i++)
                if (APTR.ReadUInt8(name, i) != APTR.ReadUInt8(secondName, i)) different = true;
            ok = different;
        }
        PublicShellTaskIdentity.Release(secondReservation);
        var flags = 0u;
        var cliNumber = 0u;
        if (ok)
        {
            APTR.WriteUInt32(message, ExecLayout.Message.ReplyPort, reply.Raw);
            APTR.WriteUInt16(message, ExecLayout.Message.Length, (ushort)MessageBytes);
            Tag(tags, 0, DosNewProcessTag.Entry, APTR.ExportAddress("copperos.shell.exit-lifecycle-worker").Raw);
            Tag(tags, 1, DosNewProcessTag.Cli, 1);
            Tag(tags, 2, DosNewProcessTag.StackSize, 16_384);
            Tag(tags, 3, DosNewProcessTag.Name, name.Raw);
            Tag(tags, 4, DosNewProcessTag.ExitCode, APTR.ExportAddress("copperos.shell.exit-lifecycle-callback").Raw);
            Tag(tags, 5, DosNewProcessTag.ExitData, message.Raw);
            var child = ChildProbeDosVectors.CreateNewProc(dos, tags);
            if (child.IsNull) ok = false;
            else
            {
                // CreateNewProc must copy NP_Name. Temporarily change the
                // caller's buffer while the child is blocked, then search for
                // the original name without allowing either task to run.
                for (var i = 0; i < (int)PublicShellTaskIdentity.NameBytes; i++)
                    APTR.WriteUInt8(secondName, i, APTR.ReadUInt8(name, i));
                global::Amiga.Exec.Forbid();
                var first = APTR.ReadUInt8(name, 0);
                APTR.WriteUInt8(name, 0, (byte)'X');
                var copiedName = global::Amiga.Exec.FindTask(CString.FromPointer(secondName.Raw)) == child;
                APTR.WriteUInt8(name, 0, first);
                global::Amiga.Exec.Permit();
                ok = copiedName && ok;
                PublicShellTaskIdentity.Attach(child, reservation);
                reservation = APTR.Null;
                var memory = new CopperSharpNativeDosPlatform();
                cliNumber = unchecked((uint)DosProcessCodec.ReadTaskNumber(ref memory, child));
                global::Amiga.Exec.PutMsg(DosProcessCodec.MessagePortAddress(child), message);
                _ = global::Amiga.Exec.WaitPort(reply);
                ok = global::Amiga.Exec.GetMsg(reply) == message && ok;
                var callbackTask = APTR.FromPointer(APTR.ReadUInt32(message, 28));
                var mask = APTR.ReadUInt32(message, 24);
                // The callback deliberately waits for this signal, so the
                // child cannot disappear between observation and release.
                global::Amiga.Exec.Forbid();
                var found = global::Amiga.Exec.FindTask(CString.FromPointer(name.Raw));
                if (found == child) flags |= 1;
                if (ExitLifecycleDosVectors.FindCliProc(dos, cliNumber) == child) flags |= 2;
                if (callbackTask == child) flags |= 4;
                if (mask != 0 && found == child) global::Amiga.Exec.Signal(child, mask);
                global::Amiga.Exec.Permit();
                ok = ok && mask != 0 && callbackTask == child && found == child;
                // DOS still executes this HUNK after the callback returns.
                // A timeout is enforced outside the guest, never by unloading
                // a segment while its child can still execute it.
                while (global::Amiga.Exec.FindTask(CString.FromPointer(name.Raw)).IsNotNull)
                    ChildProbeDosVectors.Delay(dos, 1);
                global::Amiga.Exec.Forbid();
                if (ExitLifecycleDosVectors.FindCliProc(dos, cliNumber).IsNull) flags |= 8;
                global::Amiga.Exec.Permit();
            }
        }
        if (message.IsNotNull)
        {
            APTR.WriteUInt32(message, 0, flags);
            APTR.WriteUInt32(message, 4, cliNumber);
            var path = Text("SYS:T/lifecycle.bin");
            var file = PublicShellFileServices.OpenOutput(dos, path, Length(path), 0);
            if (file.IsNull) ok = false;
            else
            {
                ok = PublicShellFileServices.Write(dos, file, message, MessageBytes) == (int)MessageBytes && ok;
                ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
            }
        }
        PublicShellTaskIdentity.Release(reservation);
        if (secondName.IsNotNull) global::Amiga.Exec.FreeMem(secondName.Raw, PublicShellTaskIdentity.NameBytes);
        if (name.IsNotNull) global::Amiga.Exec.FreeMem(name.Raw, PublicShellTaskIdentity.NameBytes);
        if (tags.IsNotNull) global::Amiga.Exec.FreeMem(tags.Raw, TagBytes);
        if (message.IsNotNull) global::Amiga.Exec.FreeMem(message.Raw, MessageBytes);
        if (reply.IsNotNull) global::Amiga.Exec.DeleteMsgPort(reply);
        var marker = Text(ok ? (CString)"SYS:runner-ok" : (CString)"SYS:runner-fail");
        var markerFile = PublicShellFileServices.OpenOutput(dos, marker, Length(marker), 0);
        if (markerFile.IsNotNull) _ = PublicShellFileServices.Close(dos, markerFile);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    [M68kExport("copperos.shell.exit-lifecycle-worker")]
    public static uint Worker()
    {
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var port = DosProcessCodec.MessagePortAddress(task);
        _ = global::Amiga.Exec.WaitPort(port);
        _ = global::Amiga.Exec.GetMsg(port);
        return 7;
    }

    [M68kExport("copperos.shell.exit-lifecycle-callback")]
    public static void ExitCallback([M68kRegister(M68kRegister.D0)] uint result,
        [M68kRegister(M68kRegister.D1)] uint exitData)
    {
        var message = APTR.FromPointer(exitData);
        var signal = global::Amiga.Exec.AllocSignal(-1);
        var mask = signal < 0 ? 0u : 1u << signal;
        APTR.WriteUInt32(message, 20, result);
        APTR.WriteUInt32(message, 24, mask);
        APTR.WriteUInt32(message, 28, global::Amiga.Exec.FindTask(CString.FromPointer(0)).Raw);
        global::Amiga.Exec.ReplyMsg(message);
        if (mask != 0)
        {
            _ = global::Amiga.Exec.Wait(mask);
            global::Amiga.Exec.FreeSignal(signal);
        }
    }

    private static APTR Allocate(uint bytes) => APTR.FromPointer(global::Amiga.Exec.AllocMem(bytes,
        global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
    private static APTR Text(CString text) => APTR.FromPointer(CString.ToUInt32(text));
    private static uint Length(APTR text)
    { uint length = 0; while (APTR.ReadUInt8(text, (int)length) != 0) length++; return length; }
    private static void Tag(APTR tags, int index, DosNewProcessTag tag, uint value)
    { APTR.WriteUInt32(tags, index * 8, (uint)tag); APTR.WriteUInt32(tags, index * 8 + 4, value); }
}

[AmigaLibrary("dos.library::shell-exit-lifecycle-probe", AmigaLibraryBasePolicy.CallerProvided)]
internal static class ExitLifecycleDosVectors
{
    [AmigaLvo(DosLvo.FindCliProc)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR FindCliProc([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] uint number);
}
