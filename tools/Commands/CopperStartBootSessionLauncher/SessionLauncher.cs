using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.Commands.BootSessionLauncher;

/// <summary>
/// Compiled integration driver using only public guest APIs. It is not a Shell
/// replacement or a shipped C command. The boot controller owns its image and
/// the configuration/result buffer until the child has retired.
/// </summary>
public static class SessionLauncher
{
    public static uint ImageEntry() => Magic;
    public const uint Magic = 0x4E445331; // NDS1
    public const uint RecordBytes = 160;
    // Inputs: magic0, bytes4, command64, args68, argLength72, readPath76,
    // readBuffer80, readCapacity84, childStack92, commandStack96, outputPath120,
    // optional delete-before-read path124. All strings are NUL terminated.
    // Outputs: stage8, createdChild12, done16, failureStage20, error24,
    // commandResult28, commandIoErr32, childDosBase36, childTask40, cli44,
    // input48, output52, restoredInput56, restoredOutput60, readBytes88,
    // parentDosBase100, launchError104, segment108, outputClose112,
    // deleteResult128, childNodeType132, childReturn136, readClose140,
    // parentTask144, joinSignalMask148, exitCallbackCount152, exitCallbackCode156.
    [M68kExport("copperos.boot-session.start")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Start([M68kRegister(M68kRegister.A0)] uint resultAddress)
    {
        var result = APTR.FromPointer(resultAddress);
        if (!Valid(result)) return 0;
        Put(result, 8, 1);
        var dos = Exec.OpenLibraryRaw("dos.library", 36);
        Put(result, 100, dos.Raw);
        if (dos.IsNull) { Failure(result, 1, 0); Put(result, 16, 1); return 0; }
        DOS.DOSLibraryBase = dos;
        const uint tagBytes = 80;
        var tags = Exec.AllocMem(tagBytes, Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (tags.IsNull)
        {
            Failure(result, 2, (uint)DOS.Error.NoFreeStore);
            Exec.CloseLibrary(dos); DOS.DOSLibraryBase = APTR.Null; Put(result, 16, 1); return 0;
        }
        var parent = Exec.FindTask(CString.FromPointer(0));
        var joinBit = Exec.AllocSignal(-1);
        if (joinBit < 0)
        {
            Exec.FreeMem(tags, tagBytes);
            Failure(result, 2, (uint)DOS.Error.NoFreeStore);
            Exec.CloseLibrary(dos); DOS.DOSLibraryBase = APTR.Null; Put(result, 16, 1); return 0;
        }
        var joinMask = 1u << joinBit;
        Put(result, 144, parent.Raw); Put(result, 148, joinMask);
        Tag(tags, 0, DosNewProcessTag.Entry, APTR.ExportAddress("copperos.boot-session.child").Raw);
        Tag(tags, 8, DosNewProcessTag.Cli, 1);
        Tag(tags, 16, DosNewProcessTag.UserData, result.Raw);
        Tag(tags, 24, DosNewProcessTag.StackSize, Get(result, 92));
        Tag(tags, 32, DosNewProcessTag.CopyVariables, 0);
        Tag(tags, 40, DosNewProcessTag.CurrentDirectory, 0);
        Tag(tags, 48, DosNewProcessTag.Name, CString.ToUInt32("CopperOS native command session"));
        Tag(tags, 56, DosNewProcessTag.ExitCode, APTR.ExportAddress("copperos.boot-session.exit").Raw);
        Tag(tags, 64, DosNewProcessTag.ExitData, result.Raw);
        var child = DOS.CreateNewProc(tags);
        Put(result, 104, unchecked((uint)DOS.IoErr()));
        Put(result, 12, child.Raw);
        Exec.FreeMem(tags, tagBytes);
        if (child.IsNull) Failure(result, 3, Get(result, 104));
        Exec.CloseLibrary(dos); DOS.DOSLibraryBase = APTR.Null;
        if (child.IsNull) Put(result, 16, 1);
        else
        {
            // Yield through public Exec until the child finishes its body.
            // The outer owner still verifies actual process retirement.
            while (Get(result, 16) == 0) Exec.Wait(joinMask);
        }
        Exec.FreeSignal(joinBit);
        return child.Raw;
    }

    [M68kExport("copperos.boot-session.child")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Child()
    {
        var task = Exec.FindTask(CString.FromPointer(0));
        if (task.IsNull) return 20;
        var result = APTR.FromPointer(APTR.ReadUInt32(task, ExecLayout.Task.UserData));
        if (!Valid(result)) return 20;
        Put(result, 8, 10); Put(result, 40, task.Raw);
        Put(result, 132, APTR.ReadUInt8(task, ExecLayout.Node.Type));
        var dos = Exec.OpenLibraryRaw("dos.library", 36);
        Put(result, 36, dos.Raw);
        if (dos.IsNull) { Failure(result, 10, 0); return CompleteChild(result, 20); }
        DOS.DOSLibraryBase = dos;
        var code = Run(result);
        Put(result, 136, code);
        Exec.CloseLibrary(dos); DOS.DOSLibraryBase = APTR.Null;
        return CompleteChild(result, code);
    }

    private static uint CompleteChild(APTR result, uint code)
    {
        Put(result, 16, 1);
        Exec.Signal(APTR.FromPointer(Get(result, 144)), Get(result, 148));
        return code;
    }

    private static uint Run(APTR result)
    {
        Put(result, 44, DOS.Cli().Raw);
        var input = DOS.Input();
        var output = DOS.Output();
        Put(result, 48, input.Raw); Put(result, 52, output.Raw);
        if (Get(result, 132) != (uint)NodeType.Process || Get(result, 44) == 0 ||
            input.IsNull || output.IsNull)
        { Failure(result, 11, unchecked((uint)DOS.IoErr())); return 20; }
        var ownedOutput = BPTR.Null;
        if (Get(result, 120) != 0)
        {
            ownedOutput = DOS.OpenRaw(Text(result, 120), DOS.FileMode.NewFile);
            if (ownedOutput.IsNull) { Failure(result, 12, unchecked((uint)DOS.IoErr())); return 20; }
            DOS.SelectOutput(ownedOutput);
        }
        Put(result, 8, 20);
        var loaded = DOS.LoadSeg(Text(result, 64));
        var segment = loaded.GetValueOrDefault();
        Put(result, 108, segment.Raw);
        if (segment.IsNull)
        {
            Failure(result, 20, unchecked((uint)DOS.IoErr()));
            RestoreOutput(result, output, ownedOutput); return 20;
        }
        Put(result, 8, 30);
        var commandResult = DOS.RunCommand(segment, unchecked((int)Get(result, 96)),
            APTR.FromPointer(Get(result, 68)), unchecked((int)Get(result, 72)));
        Put(result, 28, unchecked((uint)commandResult));
        Put(result, 32, unchecked((uint)DOS.IoErr()));
        Put(result, 8, 40);
        DOS.UnLoadSeg(segment);
        RestoreOutput(result, output, ownedOutput);
        Put(result, 56, DOS.Input().Raw); Put(result, 60, DOS.Output().Raw);
        if (Get(result, 124) != 0)
        {
            var deleted = DOS.DeleteFile(Text(result, 124));
            Put(result, 128, unchecked((uint)deleted));
            if (deleted == 0) { Failure(result, 41, unchecked((uint)DOS.IoErr())); return 20; }
        }
        Put(result, 8, 50);
        if (Get(result, 76) != 0)
        {
            var handle = DOS.OpenRaw(Text(result, 76), DOS.FileMode.OldFile);
            if (handle.IsNull) { Failure(result, 50, unchecked((uint)DOS.IoErr())); return 20; }
            var count = DOS.Read(handle, APTR.FromPointer(Get(result, 80)), unchecked((int)Get(result, 84)));
            Put(result, 88, unchecked((uint)count));
            var error = unchecked((uint)DOS.IoErr());
            Put(result, 140, unchecked((uint)DOS.Close(handle)));
            if (count < 0) { Failure(result, 51, error); return 20; }
        }
        Put(result, 8, 100);
        return unchecked((uint)commandResult);
    }

    [M68kExport("copperos.boot-session.exit")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint ExitCallback([M68kRegister(M68kRegister.D0)] uint code,
        [M68kRegister(M68kRegister.D1)] uint data)
    {
        var result = APTR.FromPointer(data);
        if (!Valid(result)) return 20;
        Put(result, 152, Get(result, 152) + 1);
        Put(result, 156, code);
        return code;
    }

    private static void RestoreOutput(APTR result, BPTR previous, BPTR owned)
    {
        if (owned.IsNull) return;
        DOS.SelectOutput(previous);
        Put(result, 112, unchecked((uint)DOS.Close(owned)));
    }
    private static bool Valid(APTR result) => result.IsNotNull &&
        (result.Raw & 3) == 0 && Get(result, 0) == Magic && Get(result, 4) >= RecordBytes;
    private static void Failure(APTR result, uint stage, uint error)
    { Put(result, 20, stage); Put(result, 24, error); }
    private static void Tag(APTR tags, int offset, DosNewProcessTag tag, uint value)
    { APTR.WriteUInt32(tags, offset, (uint)tag); APTR.WriteUInt32(tags, offset + 4, value); }
    private static CString Text(APTR result, int offset) => CString.FromPointer(Get(result, offset));
    private static uint Get(APTR result, int offset) => APTR.ReadUInt32(result, offset);
    private static void Put(APTR result, int offset, uint value) => APTR.WriteUInt32(result, offset, value);
}
