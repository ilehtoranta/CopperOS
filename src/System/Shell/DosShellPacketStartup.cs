using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>Initializes and retires a DOS packet-started Shell using public vectors.</summary>
internal static class DosShellPacketStartup
{
    public static uint Run(APTR task)
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        var packet = ShellPacketDosVectors.WaitPkt(dos);
        if (packet.IsNull) { global::Amiga.Exec.CloseLibrary(dos); return 20; }
        var guest = new CopperSharpNativeDosPlatform();
        var segments = DosProcessCodec.ReadSegmentList(ref guest, task).Address;
        if (segments.IsNotNull && guest.IsMapped(segments, 20) && APTR.ReadUInt32(segments, 16) == 0)
        {
            APTR.WriteUInt32(segments, 16, APTR.ReadUInt32(segments, 12));
            APTR.WriteUInt32(segments, 12, 0);
        }
        var kind = (APTR.ReadUInt32(packet, 12) != 0 ? 2 : 0) |
                   (APTR.ReadUInt32(packet, 16) != 0 ? 1 : 0);
        if (kind != 0 && kind != 2)
        {
            ReplyOriginal(dos, packet);
            global::Amiga.Exec.CloseLibrary(dos);
            return 20;
        }
        var flags = kind == 0 ? ShellPacketDosVectors.CliInitRun(dos, packet) :
            ShellPacketDosVectors.CliInitNewcli(dos, packet);
        var valid = (unchecked((uint)flags) & 0x80000000u) != 0;
        if (!valid && unchecked((uint)PublicShellFileServices.GetError(dos)) == task.Raw)
        {
            // CliInit already returned the packet on this error path.
            global::Amiga.Exec.CloseLibrary(dos);
            return 20;
        }
        if ((!valid && kind == 0) || kind == 2 || (valid && (flags & 8) != 0))
        {
            ReplyOriginal(dos, packet);
            packet = APTR.Null;
        }
        var cli = DosProcessCodec.ReadCommandLineInterface(ref guest, task).Address;
        if (!DosCommandLineInterfaceCodec.IsMapped(ref guest, cli))
        {
            if (packet.IsNotNull)
                ShellPacketDosVectors.ReplyPkt(dos, packet, 20, PublicShellFileServices.GetError(dos));
            global::Amiga.Exec.CloseLibrary(dos);
            return 20;
        }
        var cliData = DosCommandLineInterfaceCodec.Read(ref guest, cli);
        DosCommandLineInterfaceCodec.WriteReturnCode(ref guest, cli, 0);
        DosCommandLineInterfaceCodec.WriteResult2(ref guest, cli, 0);
        if (cliData.CurrentInput.IsNotNull) NativeForegroundDosVectors.SelectInput(dos, cliData.CurrentInput);
        if (cliData.StandardOutput.IsNotNull) NativeForegroundDosVectors.SelectOutput(dos, cliData.StandardOutput);
        if (valid && (flags & 4) != 0)
            DosCommandLineInterfaceCodec.WriteInteractive(ref guest, cli, 0);
        // Execute resumes cli_StandardInput after its command stream ends;
        // System terminates at the end of that stream.
        _ = DosShellNativeChildCore.RunFromInitializedCli(kind == 2 ||
            (valid && (flags & 4) == 0));
        cliData = DosCommandLineInterfaceCodec.Read(ref guest, cli);
        var result = cliData.ReturnCode;
        var result2 = cliData.Result2;
        var output = PublicShellFileServices.Output(dos);
        if (output.IsNotNull) ShellPacketDosVectors.Flush(dos, output);
        if (cliData.CurrentInput.IsNotNull && cliData.CurrentInput != cliData.StandardInput &&
            cliData.CurrentInput != cliData.StandardOutput)
        {
            if (cliData.StandardInput.IsNotNull) NativeForegroundDosVectors.SelectInput(dos, cliData.StandardInput);
            _ = PublicShellFileServices.Close(dos, cliData.CurrentInput);
        }
        var asynchronous = valid && (flags & 8) != 0;
        if ((!valid || asynchronous || (flags & 2) == 0) && cliData.StandardInput.IsNotNull)
            _ = PublicShellFileServices.Close(dos, cliData.StandardInput);
        if ((!valid || asynchronous || (flags & 1) != 0) && cliData.StandardOutput.IsNotNull &&
            cliData.StandardOutput != cliData.StandardInput)
            _ = PublicShellFileServices.Close(dos, cliData.StandardOutput);
        if (packet.IsNotNull) ShellPacketDosVectors.ReplyPkt(dos, packet, result, result2);
        var directory = PublicShellPathVectors.CurrentDir(dos, BPTR.Null);
        if (directory.IsNotNull) PublicShellPathVectors.UnLock(dos, directory);
        global::Amiga.Exec.CloseLibrary(dos);
        return unchecked((uint)result);
    }

    private static void ReplyOriginal(APTR dos, APTR packet) =>
        ShellPacketDosVectors.ReplyPkt(dos, packet,
            unchecked((int)APTR.ReadUInt32(packet, 12)), unchecked((int)APTR.ReadUInt32(packet, 16)));
}

[AmigaLibrary("dos.library::shell-startup", AmigaLibraryBasePolicy.CallerProvided)]
internal static class ShellPacketDosVectors
{
    [AmigaLvo(DosLvo.WaitPkt)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR WaitPkt([M68kRegister(M68kRegister.A6)] APTR dos);
    [AmigaLvo(DosLvo.ReplyPkt)]
    internal static extern void ReplyPkt([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR packet, [M68kRegister(M68kRegister.D2)] int result1,
        [M68kRegister(M68kRegister.D3)] int result2);
    [AmigaLvo(DosLvo.CliInitRun)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int CliInitRun([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.A0)] APTR packet);
    [AmigaLvo(DosLvo.CliInitNewcli)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int CliInitNewcli([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.A0)] APTR packet);
    [AmigaLvo(DosLvo.Flush)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Flush([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file);
}
