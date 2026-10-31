using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;
using CopperOS.Shell;
using CopperOS.Shell.Dos;

namespace CopperOS.SystemImage;

// Original development bootstrap. All loading, segment registration, task
// publication and console operations go through guest Exec/DOS vectors.
public static class SystemBootstrap
{
    internal const uint DirectKickstartShellMarker = 0x434F5348;

    [M68kExport("copperos.system.shell-entry")]
    public static uint ShellEntry()
    {
        var memory = new CopperSharpNativeDosPlatform();
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref memory, task).Address;
        if (!DosShellNativeChildContextCore.TryGetCliState(ref memory, cli, out var state))
        {
            if (APTR.ReadUInt32(task, ExecLayout.Task.UserData) == DirectKickstartShellMarker)
            {
                APTR.WriteUInt32(task, ExecLayout.Task.UserData, 0);
                return KickstartShell.Run();
            }
            return KickstartShell.RunDosStartup(task);
        }
        var exec = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
        if (!DosShellNativeContextCore.WriteExecBase(ref memory, state, exec)) return 20;
        var shell = new DosShellNativePlatform(state);
        // The ROM's bounded boot CLI does not construct a general Shell path.
        // Run this as the real child so DOS owns its C: locks and path lifetime.
        if (!shell.TryUpdateCommandPath(cli, Text("C:"), 3, 1, PathCommand.OperationAdd, 1)) return 20;
        return DosShellNativeEntrypoints.ShellTaskEntry();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct ProcessTags
    {
        public TagItem Segment, FreeSegment, Cli, Stack, Input, Output, Error,
            CloseInput, CloseOutput, CloseError, Name, End;
    }

    [M68kExport("copperos.system.bootstrap")]
    public static uint Boot()
    {
        var exec = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
        var dos = ExecVectors.OpenLibrary(exec, Text("dos.library"), 0);
        if (dos.IsNull) return 20;
        Print(dos, "CopperOS system proof 0.1.0-dev.1\n");
        var segment = SingleHunkStreamLoader.Load(exec, dos, Text("L:Shell-Seg"));
        if (segment.IsNull) { Print(dos, "Shell load failed\n"); return 20; }
        // The public system segment is retained for the machine lifetime. It
        // is not marked PURE or installed through command shipping admission.
        if (Vectors.AddSegment(dos, Text("Shell"), segment, -1) == 0)
        { SingleHunkStreamLoader.Release(exec, segment); Print(dos, "Shell registration failed\n"); return 20; }
        Print(dos, "CopperOS Shell registered\n");
        var parent = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var bootMemory = new CopperSharpNativeDosPlatform();
        var parentCli = DosProcessCodec.ReadCommandLineInterface(ref bootMemory, parent).Address;
        var consoleInput = DosCommandLineInterfaceCodec.Read(ref bootMemory, parentCli).StandardInput;
        if (consoleInput.IsNull) consoleInput = Vectors.Input(dos);
        var tags = new ProcessTags
        {
            Segment = TagItem.Create((uint)DosNewProcessTag.SegmentList, segment.Raw),
            FreeSegment = TagItem.Create((uint)DosNewProcessTag.FreeSegmentList, 0),
            Cli = TagItem.Create((uint)DosNewProcessTag.Cli, 1),
            Stack = TagItem.Create((uint)DosNewProcessTag.StackSize, 65536),
            Input = TagItem.Create((uint)DosNewProcessTag.Input, consoleInput.Raw),
            Output = TagItem.Create((uint)DosNewProcessTag.Output, Vectors.Output(dos).Raw),
            Error = TagItem.Create((uint)DosNewProcessTag.Error, Vectors.Output(dos).Raw),
            CloseInput = TagItem.Create((uint)DosNewProcessTag.CloseInput, 0),
            CloseOutput = TagItem.Create((uint)DosNewProcessTag.CloseOutput, 0),
            CloseError = TagItem.Create((uint)DosNewProcessTag.CloseError, 0),
            Name = TagItem.Create((uint)DosNewProcessTag.Name, Text("CopperOS Shell").Raw),
        };
        // Set the child CLI's public interactive fields before it can run.
        global::Amiga.Exec.Forbid();
        var child = Vectors.CreateNewProc(dos, TagItem.AddressOf(ref tags.Segment));
        if (child.IsNotNull)
        {
            var memory = new CopperSharpNativeDosPlatform();
            var cli = DosProcessCodec.ReadCommandLineInterface(ref memory, child).Address;
            DosCommandLineInterfaceCodec.WriteInteractive(ref memory, cli, 1);
            DosCommandLineInterfaceCodec.WriteBackground(ref memory, cli, 0);
            if (!DosShellNativeChildContextCore.TryGetCliState(ref bootMemory, parentCli, out _))
                APTR.WriteUInt32(child, ExecLayout.Task.UserData, DirectKickstartShellMarker);
        }
        global::Amiga.Exec.Permit();
        if (child.IsNull) { Print(dos, "Shell Process creation failed\n"); return 20; }
        // The boot-script Process retains its borrowed handles and segment
        // ownership, but never competes with the Shell for console input.
        while (true) global::Amiga.Exec.Wait(0);
    }

    [M68kExport("copperos.system.demo")]
    public static uint Demonstration()
    {
        var exec = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
        var dos = ExecVectors.OpenLibrary(exec, Text("dos.library"), 0);
        if (dos.IsNull) return 20;
        Print(dos, "Original CopperOS native demonstration\nCommand returned to CopperOS Shell\n");
        ExecVectors.CloseLibrary(exec, dos);
        return 0;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
    internal static void Print(APTR dos, CString value)
    {
        var text = Text(value); var length = 0;
        while (APTR.ReadUInt8(text, length) != 0) length++;
        Vectors.Write(dos, Vectors.Output(dos), text, length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [M68kExport("copperstart.dos.process-return")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint ProcessReturn([M68kRegister(M68kRegister.D0)] uint result)
        => DosNativeProcessReturn.Entry(result);
}

[AmigaLibrary("exec.library::system-bootstrap", AmigaLibraryBasePolicy.CallerProvided)]
internal static class ExecVectors
{
    [AmigaLvo(ExecLvo.OpenLibrary)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR OpenLibrary([M68kRegister(M68kRegister.A6)] APTR exec,
        [M68kRegister(M68kRegister.A1)] APTR name, [M68kRegister(M68kRegister.D0)] uint version);
    [AmigaLvo(ExecLvo.CloseLibrary)]
    internal static extern void CloseLibrary([M68kRegister(M68kRegister.A6)] APTR exec,
        [M68kRegister(M68kRegister.A1)] APTR library);
    [AmigaLvo(ExecLvo.AllocMem)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR AllocMem([M68kRegister(M68kRegister.A6)] APTR exec,
        [M68kRegister(M68kRegister.D0)] uint bytes, [M68kRegister(M68kRegister.D1)] uint flags);
    [AmigaLvo(ExecLvo.FreeMem)]
    internal static extern void FreeMem([M68kRegister(M68kRegister.A6)] APTR exec,
        [M68kRegister(M68kRegister.A1)] APTR memory, [M68kRegister(M68kRegister.D0)] uint bytes);
}

[AmigaLibrary("dos.library::system-bootstrap", AmigaLibraryBasePolicy.CallerProvided)]
internal static class Vectors
{
    [AmigaLvo(DosLvo.LockDosList)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR LockDosList([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] uint flags);
    [AmigaLvo(DosLvo.FindDosEntry)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR FindDosEntry([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR list, [M68kRegister(M68kRegister.D2)] APTR name,
        [M68kRegister(M68kRegister.D3)] uint flags);
    [AmigaLvo(DosLvo.UnLockDosList)]
    internal static extern void UnLockDosList([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] uint flags);
    [AmigaLvo(DosLvo.Open)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR Open([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR path, [M68kRegister(M68kRegister.D2)] int mode);
    [AmigaLvo(DosLvo.Close)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Close([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file);
    [AmigaLvo(DosLvo.Read)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Read([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file, [M68kRegister(M68kRegister.D2)] APTR memory,
        [M68kRegister(M68kRegister.D3)] int bytes);
    [AmigaLvo(DosLvo.LoadSeg)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint LoadSeg([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name);
    [AmigaLvo(DosLvo.UnLoadSeg)]
    internal static extern void UnLoadSeg([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR segment);
    [AmigaLvo(DosLvo.AddSegment)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int AddSegment([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name, [M68kRegister(M68kRegister.D2)] BPTR segment,
        [M68kRegister(M68kRegister.D3)] int system);
    [AmigaLvo(DosLvo.CreateNewProc)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR CreateNewProc([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR tags);
    [AmigaLvo(DosLvo.Input)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR Input([M68kRegister(M68kRegister.A6)] APTR dos);
    [AmigaLvo(DosLvo.Output)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR Output([M68kRegister(M68kRegister.A6)] APTR dos);
    [AmigaLvo(DosLvo.Write)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Write([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file, [M68kRegister(M68kRegister.D2)] APTR bytes,
        [M68kRegister(M68kRegister.D3)] int length);
}
