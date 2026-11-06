using Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>Runs the classic SetEnv/UnsetEnv listing through the public command path.</summary>
internal static class PublicShellGlobalListing
{
    private const uint ScratchSize = 512;

    public static bool Execute(CopperSharpNativeDosPlatform memory,
        BPTR input, BPTR output, BPTR error, out int result)
    {
        result = (int)ShellCommandResult.Fail;
        var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (library.IsNull) return false;
        var process = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = APTR.Null;
        if (process.IsNotNull)
            cli = DosProcessCodec.ReadCommandLineInterface(ref memory, process).Address;
        if (output.IsNull || cli.IsNull || !DosCommandLineInterfaceCodec.IsMapped(ref memory, cli))
        {
            PublicShellFileServices.SetError(library, DOS.Error.ObjectWrongType);
            global::Amiga.Exec.CloseLibrary(library);
            return false;
        }
        var selectedInput = input;
        if (selectedInput.IsNull) selectedInput = PublicShellFileServices.Input(library);
        var selectedError = error;
        if (selectedError.IsNull) selectedError = output;
        var scratch = memory.AllocateGuest(ScratchSize);
        if (scratch.IsNull || !memory.IsMapped(scratch, ScratchSize))
        {
            if (scratch.IsNotNull) memory.FreeGuest(scratch, ScratchSize);
            PublicShellFileServices.SetError(library, DOS.Error.NoFreeStore);
            global::Amiga.Exec.CloseLibrary(library);
            return false;
        }
        var name = Text("LIST");
        var arguments = Text("env: QUICK NOHEAD FILES\n");
        var workspace = new ShellScriptLookupWorkspace(scratch, ScratchSize);
        var success = PublicShellCommandLookup.Find(ref memory, library, cli, name, 4,
            in workspace, out var lookup);
        var childResult = (int)ShellCommandResult.Error;
        if (success && lookup.Kind == ShellScriptLookupKind.NotFound)
            success = WriteLiteral(ref memory, library, selectedError, Text("LIST: Unknown command\n"));
        else if (success && lookup.Kind != ShellScriptLookupKind.Malformed)
        {
            var command = default(ShellScriptCommandInvocation);
            command.CommandName = name;
            command.CommandNameLength = 4;
            command.Arguments = new ShellScriptTextSlice(arguments, 24);
            var executed = lookup.Kind == ShellScriptLookupKind.Script
                ? DosShellNativeForegroundCore.ExecuteScript(memory, cli, in command,
                    in lookup, selectedInput, output, selectedError, out childResult)
                : DosShellNativeForegroundCore.Execute(memory, cli, in command,
                    in lookup, selectedInput, output, selectedError, out childResult);
            if (!executed) childResult = (int)ShellCommandResult.Error;
        }
        else if (success) success = false;
        if (success && childResult >= (int)ShellCommandResult.Error)
            success = WriteFailure(ref memory, library, selectedError, scratch, childResult);
        if (success)
        {
            result = (int)ShellCommandResult.Ok;
            // Original V40 CLI captures publish neither LIST's return code
            // nor its secondary error, including command lookup failure.
            PublicShellFileServices.SetError(library, DOS.Error.None);
        }
        var savedError = PublicShellFileServices.GetError(library);
        memory.FreeGuest(scratch, ScratchSize);
        PublicShellFileServices.SetError(library, (DOS.Error)savedError);
        global::Amiga.Exec.CloseLibrary(library);
        return success;
    }

    private static bool WriteLiteral(ref CopperSharpNativeDosPlatform memory, APTR dos,
        BPTR output, APTR text)
    {
        var length = 0u;
        while (memory.ReadUInt8(text, (int)length) != 0) length++;
        return PublicShellFileServices.Write(dos, output, text, length) == (int)length;
    }

    private static bool WriteFailure(ref CopperSharpNativeDosPlatform memory, APTR dos,
        BPTR output, APTR buffer, int value)
    {
        var prefix = Text("LIST failed returncode ");
        var length = 0u;
        while (memory.ReadUInt8(prefix, (int)length) != 0)
        {
            memory.WriteUInt8(buffer, (int)length, memory.ReadUInt8(prefix, (int)length));
            length++;
        }
        var number = unchecked((uint)value);
        var divisor = 1u;
        while (divisor <= number / 10u) divisor *= 10u;
        while (divisor != 0)
        {
            memory.WriteUInt8(buffer, (int)length++, (byte)('0' + number / divisor));
            number %= divisor;
            divisor /= 10u;
        }
        memory.WriteUInt8(buffer, (int)length++, (byte)'\n');
        return PublicShellFileServices.Write(dos, output, buffer, length) == (int)length;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}
