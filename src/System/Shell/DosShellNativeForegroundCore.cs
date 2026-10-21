using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>Runs a foreground disk command in its calling Shell Process.</summary>
internal static class DosShellNativeForegroundCore
{
    public static bool ExecuteScript(CopperSharpNativeDosPlatform dos, APTR state,
        APTR cli, in ShellScriptCommandInvocation command,
        in ShellScriptLookupResult lookup, BPTR input, BPTR output, BPTR error,
        out int result)
    {
        result = (int)ShellCommandResult.Error;
        var capacity = 12 + lookup.PathLength * 2 + command.Arguments.Length;
        var line = dos.AllocateGuest(capacity);
        var name = dos.AllocateGuest(8);
        var path = dos.AllocateGuest(512);
        if (line.IsNull || name.IsNull || path.IsNull)
        {
            if (line.IsNotNull) dos.FreeGuest(line, capacity);
            if (name.IsNotNull) dos.FreeGuest(name, 8);
            if (path.IsNotNull) dos.FreeGuest(path, 512);
            DosCore.SetIoErr(ref dos, state, DOS.Error.NoFreeStore);
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
        var found = DosShellNativeBridge.LookupCommand(ref dos, state, cli,
            name, 7, path, 512, out var resolved);
        var executed = false;
        if (found && resolved.Kind is not (DosShellNativeBridge.LookupKind.NotFound or
            DosShellNativeBridge.LookupKind.Malformed or DosShellNativeBridge.LookupKind.Script))
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
            executable.Kind = resolved.Kind == DosShellNativeBridge.LookupKind.Resident
                ? ShellScriptLookupKind.Resident : ShellScriptLookupKind.ExplicitFile;
            executed = Execute(dos, state, cli, in invocation, in executable,
                input, output, error, out result);
        }
        var ioError = DosCore.IoErr(ref dos, state);
        dos.FreeGuest(line, capacity);
        dos.FreeGuest(name, 8);
        dos.FreeGuest(path, 512);
        DosCore.SetIoErr(ref dos, state, ioError);
        return executed;
    }

    public static bool Execute(CopperSharpNativeDosPlatform dos, APTR state,
        APTR cli, in ShellScriptCommandInvocation command,
        in ShellScriptLookupResult lookup, BPTR input, BPTR output, BPTR error,
        out int result)
    {
        result = (int)ShellCommandResult.Error;
        var path = dos.AllocateGuest(lookup.PathLength + 1);
        if (path.IsNull) { DosCore.SetIoErr(ref dos, state, DOS.Error.NoFreeStore); return false; }
        dos.Copy(lookup.ResolvedPath, path, lookup.PathLength);
        dos.WriteUInt8(path, (int)lookup.PathLength, 0);
        var resident = APTR.Null;
        var segment = BPTR.Null;
        if (lookup.Kind == ShellScriptLookupKind.Resident)
        {
            var raw = DosShellNativeBridge.LookupResidentNativeRaw(ref dos,
                state, command.CommandName, command.CommandNameLength);
            segment = BPTR.FromRaw(DosShellNativeBridge.AcquireResidentNativeRaw(
                ref dos, state, command.CommandName, command.CommandNameLength, raw));
            if (segment.IsNotNull) resident = APTR.FromPointer(raw);
        }
        else segment = DosSegmentLoaderCore.Load(ref dos, state, path);
        var loadError = DosCore.IoErr(ref dos, state);
        dos.FreeGuest(path, lookup.PathLength + 1);
        if (segment.IsNull) { DosCore.SetIoErr(ref dos, state, loadError); return false; }

        var arguments = command.Arguments;
        while (arguments.Length != 0 && dos.ReadUInt8(arguments.Data) is (byte)' ' or (byte)'\t')
        { arguments.Data = APTR.FromPointer(arguments.Data.Raw + 1); arguments.Length--; }
        var length = arguments.Length;
        var append = length == 0 || dos.ReadUInt8(arguments.Data, (int)length - 1) != (byte)'\n';
        var argumentCapacity = length + 2;
        var capacity = argumentCapacity + 256;
        var text = dos.AllocateGuest(capacity);
        var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 0);
        if (text.IsNull || library.IsNull)
        {
            if (text.IsNotNull) dos.FreeGuest(text, capacity);
            if (library.IsNotNull) global::Amiga.Exec.CloseLibrary(library);
            Release(ref dos, state, segment, resident);
            DosCore.SetIoErr(ref dos, state, DOS.Error.NoFreeStore);
            return false;
        }
        if (length != 0) dos.Copy(arguments.Data, text, length);
        if (append) dos.WriteUInt8(text, (int)length++, (byte)'\n');
        dos.WriteUInt8(text, (int)length, 0);
        var savedName = APTR.FromPointer(text.Raw + argumentCapacity);
        var previousName = DosCommandLineInterfaceCodec.Read(ref dos, cli).CommandName.Address;
        var previousLength = previousName.IsNull ? 0u : dos.ReadUInt8(previousName);
        for (var i = 0u; i < previousLength; i++)
            dos.WriteUInt8(savedName, (int)i, dos.ReadUInt8(previousName, (int)i + 1));
        dos.WriteUInt8(savedName, (int)previousLength, 0);
        DosObjectCore.SetCliString(ref dos, state, cli, (uint)DosObjectTag.CliCommandName,
            command.CommandName, command.CommandNameLength);
        var task = dos.CurrentDosTask;
        var savedInput = DosCore.SelectInput(ref dos, state, input);
        var savedOutput = DosCore.SelectOutput(ref dos, state, output);
        var savedError = DosProcessCodec.ReadCurrentError(ref dos, task);
        DosProcessCodec.WriteCurrentError(ref dos, task, error);
        var stack = DosCommandLineInterfaceCodec.Read(ref dos, cli).DefaultStack;
        // cli_Module belongs to the Shell. RunCommand preserves it even for
        // nested calls; commands can use it to detach a worker HUNK before
        // returning and letting the Shell unload the remaining command chain.
        var savedModule = dos.ReadUInt32(cli, DosLayout.CommandLineInterface.Module);
        dos.WriteUInt32(cli, DosLayout.CommandLineInterface.Module, segment.Raw);
        result = NativeForegroundDosVectors.RunCommand(library, segment,
            stack == 0 ? 4096 : unchecked(stack << 2), text, (int)length);
        dos.WriteUInt32(cli, DosLayout.CommandLineInterface.Module, savedModule);
        var ioError = DosCore.IoErr(ref dos, state);
        DosCore.SelectInput(ref dos, state, savedInput);
        DosCore.SelectOutput(ref dos, state, savedOutput);
        DosProcessCodec.WriteCurrentError(ref dos, task, savedError);
        DosObjectCore.SetCliString(ref dos, state, cli, (uint)DosObjectTag.CliCommandName,
            savedName, previousLength);
        global::Amiga.Exec.CloseLibrary(library);
        dos.FreeGuest(text, capacity);
        Release(ref dos, state, segment, resident);
        DosCore.SetIoErr(ref dos, state, ioError);
        return true;
    }

    private static void Release(ref CopperSharpNativeDosPlatform dos, APTR state,
        BPTR segment, APTR resident)
    {
        if (resident.IsNotNull) DosShellNativeBridge.ReleaseResidentNativeRaw(ref dos, state, resident.Raw);
        else DosSegmentLoaderCore.Unload(ref dos, state, segment);
    }
}

[AmigaLibrary("dos.library::shell-foreground", AmigaLibraryBasePolicy.CallerProvided)]
internal static class NativeForegroundDosVectors
{
    [AmigaLvo(DosLvo.RunCommand)]
    [return: M68kRegister(M68kRegister.D0)]
    internal static extern int RunCommand([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.D1)] BPTR segment, [M68kRegister(M68kRegister.D2)] int stack,
        [M68kRegister(M68kRegister.D3)] APTR parameters, [M68kRegister(M68kRegister.D4)] int length);
}
