using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Observes transfer and rollback of an explicitly supplied directory lock.</summary>
public static class PublicCreateProcessLockNativeProbe
{
    [M68kEntryPoint]
    [M68kExport("copperos.shell.create-process-lock-probe")]
    public static int Main()
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        var path = Text("SYS:T/create-lock-probe");
        var createdDirectory = CreateProcessLockDosVectors.CreateDir(dos, path);
        var stages = 0u;
        if (createdDirectory.IsNotNull)
        {
            PublicShellPathVectors.UnLock(dos, createdDirectory);
            var directory = PublicShellPathVectors.Lock(dos, path, -2);
            var tags = APTR.FromPointer(global::Amiga.Exec.AllocMem(40,
                global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
            if (directory.IsNotNull && tags.IsNotNull)
            {
                stages |= 1;
                Tag(tags, 0, DosNewProcessTag.Entry, APTR.ExportAddress("copperos.shell.create-process-lock-worker").Raw);
                Tag(tags, 1, DosNewProcessTag.CurrentDirectory, directory.Raw);
                // A valid but unattainable allocation forces CreateNewProc to
                // fail without exhausting memory needed by the filesystem.
                Tag(tags, 2, DosNewProcessTag.StackSize, 0x10000000);
                var child = BackgroundDosVectors.CreateNewProc(dos, tags);
                if (child.IsNull)
                {
                    stages |= 2;
                    var exclusive = PublicShellPathVectors.Lock(dos, path, -1);
                    if (exclusive.IsNull)
                    {
                        // The original lock remains held by the caller.
                        stages |= 4;
                        PublicShellPathVectors.UnLock(dos, directory);
                        exclusive = PublicShellPathVectors.Lock(dos, path, -1);
                        if (exclusive.IsNotNull) stages |= 8;
                    }
                    else stages |= 16; // DOS consumed the supplied lock.
                    if (exclusive.IsNotNull) PublicShellPathVectors.UnLock(dos, exclusive);
                }
                // A successful launch is unexpected; the child owns the lock.
                var transferred = PublicShellPathVectors.Lock(dos, path, -2);
                var message = APTR.FromPointer(global::Amiga.Exec.AllocMem(24,
                    global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
                if (transferred.IsNotNull && message.IsNotNull)
                {
                    Tag(tags, 1, DosNewProcessTag.CurrentDirectory, transferred.Raw);
                    Tag(tags, 2, DosNewProcessTag.StackSize, 4096);
                    Tag(tags, 3, DosNewProcessTag.Name, Text("CopperOS Lock Transfer Probe").Raw);
                    var successfulChild = BackgroundDosVectors.CreateNewProc(dos, tags);
                    if (successfulChild.IsNull) PublicShellPathVectors.UnLock(dos, transferred);
                    else
                    {
                        stages |= 32;
                        var memory = new CopperSharpNativeDosPlatform();
                        if (DosProcessCodec.ReadCurrentDirectory(ref memory, successfulChild) == transferred)
                            stages |= 512;
                        APTR.WriteUInt16(message, ExecLayout.Message.Length, 24);
                        global::Amiga.Exec.PutMsg(DosProcessCodec.MessagePortAddress(successfulChild), message);
                        var gone = false;
                        for (var i = 0; i < 500; i++)
                        {
                            if (global::Amiga.Exec.FindTask((CString)"CopperOS Lock Transfer Probe").IsNull)
                            { gone = true; break; }
                            PublicShellContinuationDosVectors.Delay(dos, 1);
                        }
                        if (gone)
                        {
                            stages |= 64;
                            var exclusive = PublicShellPathVectors.Lock(dos, path, -1);
                            if (exclusive.IsNotNull)
                            {
                                stages |= 128;
                                PublicShellPathVectors.UnLock(dos, exclusive);
                            }
                            else
                            {
                                // The caller-supplied lock was not reclaimed.
                                // The child is gone, so release it for this
                                // disposable probe without touching its copy.
                                PublicShellPathVectors.UnLock(dos, transferred);
                                exclusive = PublicShellPathVectors.Lock(dos, path, -1);
                                if (exclusive.IsNotNull)
                                {
                                    stages |= 256;
                                    PublicShellPathVectors.UnLock(dos, exclusive);
                                }
                            }
                        }
                        else
                        {
                            // The worker executes from this probe image. Never
                            // return and unload it while the worker is alive.
                            while (global::Amiga.Exec.FindTask((CString)"CopperOS Lock Transfer Probe").IsNotNull)
                                PublicShellContinuationDosVectors.Delay(dos, 1);
                        }
                    }
                }
                else if (transferred.IsNotNull) PublicShellPathVectors.UnLock(dos, transferred);
                if (message.IsNotNull) global::Amiga.Exec.FreeMem(message.Raw, 24);
            }
            else if (directory.IsNotNull) PublicShellPathVectors.UnLock(dos, directory);
            if (tags.IsNotNull) global::Amiga.Exec.FreeMem(tags.Raw, 40);
        }
        var marker = Text(stages == 751 ? (CString)"SYS:runner-ok" : (CString)"SYS:runner-fail");
        var markerFile = PublicShellFileServices.OpenOutput(dos, marker, stages == 751 ? 13u : 15u, 0);
        if (markerFile.IsNotNull) _ = PublicShellFileServices.Close(dos, markerFile);
        var diagnostic = APTR.FromPointer(global::Amiga.Exec.AllocMem(4,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
        if (diagnostic.IsNotNull)
        {
            APTR.WriteUInt32(diagnostic, 0, stages);
            var file = PublicShellFileServices.OpenOutput(dos, Text("SYS:T/create-lock-result"), 24, 0);
            if (file.IsNotNull)
            {
                _ = PublicShellFileServices.Write(dos, file, diagnostic, 4);
                _ = PublicShellFileServices.Close(dos, file);
            }
            global::Amiga.Exec.FreeMem(diagnostic.Raw, 4);
        }
        global::Amiga.Exec.CloseLibrary(dos);
        return stages == 751 ? 0 : 10;
    }

    [M68kExport("copperos.shell.create-process-lock-worker")]
    public static uint Worker()
    {
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        if (task.IsNull) return 20;
        var port = DosProcessCodec.MessagePortAddress(task);
        _ = global::Amiga.Exec.WaitPort(port);
        return global::Amiga.Exec.GetMsg(port).IsNotNull ? 0u : 20u;
    }

    private static void Tag(APTR tags, int index, DosNewProcessTag tag, uint value)
    {
        APTR.WriteUInt32(tags, index * 8, (uint)tag);
        APTR.WriteUInt32(tags, index * 8 + 4, value);
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}

[AmigaLibrary("dos.library::create-process-lock-probe", AmigaLibraryBasePolicy.CallerProvided)]
internal static class CreateProcessLockDosVectors
{
    [AmigaLvo(DosLvo.CreateDir)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR CreateDir([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name);
}
