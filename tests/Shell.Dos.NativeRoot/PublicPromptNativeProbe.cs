using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Checks public CLI prompt access without CopperStart DOS state.</summary>
public static class PublicPromptNativeProbe
{
    [M68kEntryPoint]
    [M68kExport("copperos.shell.public-prompt-probe")]
    public static int Main()
    {
        var ok = Run();
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        #if COPPEROS_PROBE_SYS
        var marker = ok ? Text("SYS:T/runner-ok") : Text("SYS:T/runner-fail");
        var file = PublicShellFileServices.OpenOutput(dos, marker, ok ? 15u : 17u, 0);
        #else
        var marker = ok ? Text("DH0:runner-ok") : Text("DH0:runner-fail");
        var file = PublicShellFileServices.OpenOutput(dos, marker, ok ? 13u : 15u, 0);
        #endif
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    private static bool Run()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref platform, task).Address;
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        var memory = platform.AllocateRunnerMemory(512);
        if (dos.IsNull || memory.IsNull)
        {
            if (memory.IsNotNull) platform.FreeRunnerMemory(memory, 512);
            if (dos.IsNotNull) global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var before = DosCommandLineInterfaceCodec.Read(ref platform, cli);
        var previousError = PublicShellFileServices.GetError(dos);
        var directory = APTR.FromPointer(memory.Raw + 64);
        var scratch = APTR.FromPointer(memory.Raw + 128);
        var templateText = Text("%N|%S|%R|%Q");
        platform.WriteUInt8(memory, 0, 11);
        for (var i = 0; i < 11; i++) platform.WriteUInt8(memory, i + 1, platform.ReadUInt8(templateText, i));
        platform.WriteUInt8(directory, 0, 3);
        platform.WriteUInt8(directory, 1, (byte)'P');
        platform.WriteUInt8(directory, 2, (byte)':');
        platform.WriteUInt8(directory, 3, (byte)'D');
        DosCommandLineInterfaceCodec.WritePrompt(ref platform, cli, BPTR.FromAddress(memory));
        DosCommandLineInterfaceCodec.WriteCurrentDirectoryName(ref platform, cli, BPTR.FromAddress(directory));
        DosCommandLineInterfaceCodec.WriteReturnCode(ref platform, cli, -12);
        var ok = platform.TrySetPrompt(cli, Text("Hi>"), 3, 0) &&
            platform.TryCopyScriptPromptTemplate(cli, scratch, 128, out var template) &&
            template.Length == 3 && platform.ReadUInt8(scratch, 0) == (byte)'H';
        ok = platform.TrySetPrompt(cli, APTR.Null, 0, 1) &&
            platform.TryCopyScriptPromptTemplate(cli, scratch, 128, out template) &&
            template.Length == 6 && platform.ReadUInt8(scratch, 0) == (byte)'%' &&
            platform.ReadUInt8(scratch, 5) == (byte)'>' && ok;
        platform.WriteUInt8(scratch, 0, (byte)'O');
        platform.WriteUInt8(scratch, 1, (byte)'K');
        platform.WriteUInt8(scratch, 2, (byte)'X');
        ok = platform.TrySetPrompt(cli, scratch, 2, 0) &&
            platform.TryCopyScriptPromptTemplate(cli, scratch, 128, out template) &&
            template.Length == 2 && platform.ReadUInt8(scratch, 0) == (byte)'O' &&
            platform.ReadUInt8(scratch, 1) == (byte)'K' && ok;
        ok = platform.TrySetPrompt(cli, templateText, 11, 0) &&
            platform.TryCopyScriptPromptTemplate(cli, scratch, 128, out template) &&
            template.Length == 11 && ok;
        for (var i = 0; i < 11; i++)
            if (platform.ReadUInt8(scratch, i) != platform.ReadUInt8(templateText, i)) ok = false;
        ok = !platform.TryCopyScriptPromptTemplate(cli, scratch, 11, out _) &&
            PublicShellFileServices.GetError(dos) == (int)DOS.Error.LineTooLong && ok;
        #if COPPEROS_PROBE_SYS
        var path = Text("SYS:T/prompt-probe"); const uint pathLength = 18;
        #else
        var path = Text("DH0:prompt-probe"); const uint pathLength = 16;
        #endif
        var file = PublicShellFileServices.OpenOutput(dos, path, pathLength, 0);
        if (file.IsNull) ok = false;
        else
        {
            ok = platform.TryWriteScriptPrompt(cli, file) && ok;
            var segment = new ShellScriptPromptSegment { Kind = ShellScriptPromptSegmentKind.Literal,
                Text = Text("<%S>"), Length = 4 };
            ok = platform.TryWriteScriptPromptLiteral(cli, file, in segment) && ok;
            segment.Text = Text("`bad`"); segment.Length = 5;
            ok = !platform.TryWriteScriptPromptLiteral(cli, file, in segment) &&
                PublicShellFileServices.GetError(dos) == (int)DOS.Error.ActionNotKnown && ok;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
            file = PublicShellFileServices.OpenInput(dos, path, pathLength);
            if (file.IsNull) ok = false;
            else
            {
                var length = PublicShellFileServices.Read(dos, file, scratch, 128);
                var number = DosProcessCodec.ReadTaskNumber(ref platform, task);
                var position = 0;
                var divisor = 1;
                while (divisor <= number / 10) divisor *= 10;
                while (divisor != 0)
                {
                    if (platform.ReadUInt8(scratch, position++) != (byte)('0' + number / divisor)) ok = false;
                    number %= divisor; divisor /= 10;
                }
                var suffix = Text("|P:D|-12|%Q<P:D>");
                for (var i = 0; i < 16; i++)
                    if (platform.ReadUInt8(scratch, position++) != platform.ReadUInt8(suffix, i)) ok = false;
                ok = length == position && PublicShellFileServices.Close(dos, file) != 0 && ok;
            }
        }
        _ = PublicShellFileServices.Delete(dos, path);
        DosCommandLineInterfaceCodec.WritePrompt(ref platform, cli, before.Prompt);
        DosCommandLineInterfaceCodec.WriteCurrentDirectoryName(ref platform, cli, before.CurrentDirectoryName);
        DosCommandLineInterfaceCodec.WriteReturnCode(ref platform, cli, before.ReturnCode);
        platform.FreeRunnerMemory(memory, 512);
        PublicShellFileServices.SetError(dos, (DOS.Error)previousError);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}
