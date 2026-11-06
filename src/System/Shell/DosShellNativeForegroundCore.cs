using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>Runs a foreground disk command in its calling Shell Process.</summary>
internal static class DosShellNativeForegroundCore
{
    public static bool ExecuteScript(CopperSharpNativeDosPlatform dos,
        APTR cli, in ShellScriptCommandInvocation command,
        in ShellScriptLookupResult lookup, BPTR input, BPTR output, BPTR error,
        out int result)
    {
        result = (int)ShellCommandResult.Error;
        var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (library.IsNull) return false;
        var capacity = 12 + lookup.PathLength * 2 + command.Arguments.Length;
        var line = dos.AllocateGuest(capacity);
        var name = dos.AllocateGuest(8);
        var path = dos.AllocateGuest(512);
        if (line.IsNull || name.IsNull || path.IsNull)
        {
            if (line.IsNotNull) dos.FreeGuest(line, capacity);
            if (name.IsNotNull) dos.FreeGuest(name, 8);
            if (path.IsNotNull) dos.FreeGuest(path, 512);
            PublicShellFileServices.SetError(library, DOS.Error.NoFreeStore);
            global::Amiga.Exec.CloseLibrary(library);
            return false;
        }
        dos.WriteUInt32(name, 0, 0x45786563);
        dos.WriteUInt32(name, 4, 0x75746500);
        dos.Copy(name, line, 7);
        dos.WriteUInt8(line, 7, (byte)' ');
        dos.WriteUInt8(line, 8, (byte)'"');
        var position = 9u;
        for (var i = 0u; i < lookup.PathLength; i++)
        {
            var value = dos.ReadUInt8(lookup.ResolvedPath, (int)i);
            if (value is (byte)'*' or (byte)'"') dos.WriteUInt8(line, (int)position++, (byte)'*');
            dos.WriteUInt8(line, (int)position++, value);
        }
        dos.WriteUInt8(line, (int)position++, (byte)'"');
        if (command.Arguments.Length != 0)
        {
            dos.WriteUInt8(line, (int)position++, (byte)' ');
            dos.Copy(command.Arguments.Data, APTR.FromPointer(line.Raw + position), command.Arguments.Length);
            position += command.Arguments.Length;
        }
        dos.WriteUInt8(line, (int)position, 0);
        var workspace = new ShellScriptLookupWorkspace(path, 512);
        var found = PublicShellCommandLookup.Find(ref dos, library, cli,
            name, 7, in workspace, out var resolved);
        var executed = false;
        if (found && resolved.Kind is not (ShellScriptLookupKind.NotFound or
            ShellScriptLookupKind.Malformed or ShellScriptLookupKind.Script))
        {
            var invocation = default(ShellScriptCommandInvocation);
            invocation.Line = line;
            invocation.LineLength = position;
            invocation.CommandName = name;
            invocation.CommandNameLength = 7;
            invocation.Arguments = new ShellScriptTextSlice(APTR.FromPointer(line.Raw + 8), position - 8);
            var executable = default(ShellScriptLookupResult);
            executable.ResolvedPath = path;
            executable.PathLength = resolved.PathLength;
            executable.Kind = resolved.Kind;
            executed = Execute(dos, cli, in invocation, in executable,
                input, output, error, out result);
        }
        var ioError = PublicShellFileServices.GetError(library);
        dos.FreeGuest(line, capacity);
        dos.FreeGuest(name, 8);
        dos.FreeGuest(path, 512);
        PublicShellFileServices.SetError(library, (DOS.Error)ioError);
        global::Amiga.Exec.CloseLibrary(library);
        return executed;
    }

    public static bool Execute(CopperSharpNativeDosPlatform dos,
        APTR cli, in ShellScriptCommandInvocation command,
        in ShellScriptLookupResult lookup, BPTR input, BPTR output, BPTR error,
        out int result)
    {
        result = (int)ShellCommandResult.Error;
        var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (library.IsNull) return false;
        var path = dos.AllocateGuest(lookup.PathLength + 1);
        if (path.IsNull)
        {
            PublicShellFileServices.SetError(library, DOS.Error.NoFreeStore);
            global::Amiga.Exec.CloseLibrary(library);
            return false;
        }
        dos.Copy(lookup.ResolvedPath, path, lookup.PathLength);
        dos.WriteUInt8(path, (int)lookup.PathLength, 0);
        var resident = APTR.Null;
        var segment = BPTR.Null;
        var home = BPTR.Null;
        if (lookup.Kind == ShellScriptLookupKind.Resident)
        {
            segment = PublicShellResidentServices.Acquire(ref dos, library, path, out resident);
        }
        else
        {
            // PROGDIR names the executable's directory, independently of the
            // current directory and any command-path search that found it.
            var file = PublicShellFileServices.Lock(library, path);
            if (file.IsNotNull)
            {
                home = NativeForegroundDosVectors.ParentDir(library, file);
                var homeError = PublicShellFileServices.GetError(library);
                PublicShellFileServices.UnLock(library, file);
                PublicShellFileServices.SetError(library, (DOS.Error)homeError);
            }
            if (home.IsNotNull) segment = NativeForegroundDosVectors.LoadSeg(library, path);
        }
        var loadError = PublicShellFileServices.GetError(library);
        dos.FreeGuest(path, lookup.PathLength + 1);
        if (segment.IsNull)
        {
            if (home.IsNotNull) PublicShellFileServices.UnLock(library, home);
            PublicShellFileServices.SetError(library, (DOS.Error)loadError);
            global::Amiga.Exec.CloseLibrary(library);
            return false;
        }

        var arguments = command.Arguments;
        while (arguments.Length != 0 && dos.ReadUInt8(arguments.Data) is (byte)' ' or (byte)'\t')
        { arguments.Data = APTR.FromPointer(arguments.Data.Raw + 1); arguments.Length--; }
        var length = arguments.Length;
        var append = length == 0 || dos.ReadUInt8(arguments.Data, (int)length - 1) != (byte)'\n';
        var argumentCapacity = length + 2;
        var capacity = argumentCapacity + 512;
        var text = dos.AllocateGuest(capacity);
        if (text.IsNull)
        {
            Release(ref dos, library, segment, resident);
            if (home.IsNotNull) PublicShellFileServices.UnLock(library, home);
            PublicShellFileServices.SetError(library, DOS.Error.NoFreeStore);
            global::Amiga.Exec.CloseLibrary(library);
            return false;
        }
        if (length != 0) dos.Copy(arguments.Data, text, length);
        if (append) dos.WriteUInt8(text, (int)length++, (byte)'\n');
        dos.WriteUInt8(text, (int)length, 0);
        var savedName = APTR.FromPointer(text.Raw + argumentCapacity);
        var newName = APTR.FromPointer(savedName.Raw + 256);
        if (command.CommandNameLength > 255 || command.CommandName.IsNull ||
            NativeForegroundDosVectors.GetProgramName(library, savedName, 256) == 0)
        {
            var nameError = command.CommandNameLength > 255
                ? (int)DOS.Error.LineTooLong : PublicShellFileServices.GetError(library);
            dos.FreeGuest(text, capacity);
            Release(ref dos, library, segment, resident);
            if (home.IsNotNull) PublicShellFileServices.UnLock(library, home);
            PublicShellFileServices.SetError(library, (DOS.Error)nameError);
            global::Amiga.Exec.CloseLibrary(library);
            return false;
        }
        dos.Copy(command.CommandName, newName, command.CommandNameLength);
        dos.WriteUInt8(newName, (int)command.CommandNameLength, 0);
        if (NativeForegroundDosVectors.SetProgramName(library, newName) == 0)
        {
            var nameError = PublicShellFileServices.GetError(library);
            dos.FreeGuest(text, capacity);
            Release(ref dos, library, segment, resident);
            if (home.IsNotNull) PublicShellFileServices.UnLock(library, home);
            PublicShellFileServices.SetError(library, (DOS.Error)nameError);
            global::Amiga.Exec.CloseLibrary(library);
            return false;
        }
        var task = dos.CurrentDosTask;
        var savedInput = NativeForegroundDosVectors.SelectInput(library, input);
        var savedOutput = NativeForegroundDosVectors.SelectOutput(library, output);
        var savedError = DosProcessCodec.ReadCurrentError(ref dos, task);
        DosProcessCodec.WriteCurrentError(ref dos, task, error);
        var stack = DosCommandLineInterfaceCodec.Read(ref dos, cli).DefaultStack;
        // cli_Module belongs to the Shell. RunCommand preserves it even for
        // nested calls; commands can use it to detach a worker HUNK before
        // returning and letting the Shell unload the remaining command chain.
        var savedModule = dos.ReadUInt32(cli, DosLayout.CommandLineInterface.Module);
        dos.WriteUInt32(cli, DosLayout.CommandLineInterface.Module, segment.Raw);
        var savedHome = home.IsNotNull ? NativeForegroundDosVectors.SetProgramDir(library, home) : BPTR.Null;
        result = NativeForegroundDosVectors.RunCommand(library, segment,
            stack == 0 ? 4096 : unchecked(stack << 2), text, (int)length);
        dos.WriteUInt32(cli, DosLayout.CommandLineInterface.Module, savedModule);
        var ioError = PublicShellFileServices.GetError(library);
        if (home.IsNotNull)
        {
            var finishedHome = NativeForegroundDosVectors.SetProgramDir(library, savedHome);
            if (finishedHome.IsNotNull) PublicShellFileServices.UnLock(library, finishedHome);
        }
        NativeForegroundDosVectors.SelectInput(library, savedInput);
        NativeForegroundDosVectors.SelectOutput(library, savedOutput);
        DosProcessCodec.WriteCurrentError(ref dos, task, savedError);
        NativeForegroundDosVectors.SetProgramName(library, savedName);
        dos.FreeGuest(text, capacity);
        Release(ref dos, library, segment, resident);
        PublicShellFileServices.SetError(library, (DOS.Error)ioError);
        global::Amiga.Exec.CloseLibrary(library);
        return true;
    }

    private static void Release(ref CopperSharpNativeDosPlatform dos,
        APTR library, BPTR segment, APTR resident)
    {
        if (resident.IsNotNull) PublicShellResidentServices.Release(ref dos, resident);
        else NativeForegroundDosVectors.UnLoadSeg(library, segment);
    }
}

[AmigaLibrary("dos.library::shell-foreground", AmigaLibraryBasePolicy.CallerProvided)]
internal static class NativeForegroundDosVectors
{
    [AmigaLvo(DosLvo.LoadSeg)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR LoadSeg([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.D1)] APTR name);
    [AmigaLvo(DosLvo.UnLoadSeg)]
    internal static extern void UnLoadSeg([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.D1)] BPTR segment);
    [AmigaLvo(DosLvo.ParentDir)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR ParentDir([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.D1)] BPTR file);
    [AmigaLvo(DosLvo.SelectInput)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR SelectInput([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.D1)] BPTR file);
    [AmigaLvo(DosLvo.SelectOutput)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR SelectOutput([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.D1)] BPTR file);
    [AmigaLvo(DosLvo.SetProgramDir)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR SetProgramDir([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.D1)] BPTR directory);
    [AmigaLvo(DosLvo.SetProgramName)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetProgramName([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.D1)] APTR name);
    [AmigaLvo(DosLvo.GetProgramName)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int GetProgramName([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.D1)] APTR buffer, [M68kRegister(M68kRegister.D2)] int size);
    [AmigaLvo(DosLvo.GetProgramDir)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR GetProgramDir([M68kRegister(M68kRegister.A6)] APTR library);
    [AmigaLvo(DosLvo.RunCommand)]
    [return: M68kRegister(M68kRegister.D0)]
    internal static extern int RunCommand([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.D1)] BPTR segment, [M68kRegister(M68kRegister.D2)] int stack,
        [M68kRegister(M68kRegister.D3)] APTR parameters, [M68kRegister(M68kRegister.D4)] int length);
}
