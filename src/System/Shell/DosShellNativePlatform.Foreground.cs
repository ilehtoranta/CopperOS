using Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

public partial struct DosShellNativePlatform
{
    private bool ExecuteForeground(APTR cli, APTR frame,
        in ShellScriptCommandInvocation command, in ShellScriptLookupResult lookup,
        BPTR input, BPTR output, BPTR error, out int result, out APTR continuation)
    {
        var dos = CreateDos();
        continuation = APTR.Null;
        var executed = lookup.Kind == ShellScriptLookupKind.Script
            ? DosShellNativeForegroundCore.ExecuteScript(dos, State, cli,
                in command, in lookup, input, output, error, out result)
            : DosShellNativeForegroundCore.Execute(dos, State, cli,
                in command, in lookup, input, output, error, out result);
        var savedError = DosCore.IoErr(ref dos, State);
        // Loader/I/O/allocation errors are command failures. The interactive
        // Shell keeps reading after publishing their DOS error.
        if (!executed) result = (int)ShellCommandResult.Error;
        var adopted = !executed || AdoptNativeCommandInput(cli, frame);
        DosCore.SetIoErr(ref dos, State, savedError);
        return adopted;
    }

    private bool AdoptNativeCommandInput(APTR cli, APTR frame)
    {
        var dos = CreateDos();
        var runner = DosShellNativeBridge.FindScriptRunner(ref dos, State, cli);
        if (runner.IsNull || !DosShellNativeBridge.ReadScriptRunner(ref dos,
            State, runner, out var stored) || stored.Frame != frame) return false;
        var current = DosCommandLineInterfaceCodec.Read(ref dos, cli);
        if (current.CurrentInput.IsNull) return true;
        if (current.CurrentInput == current.StandardInput)
        {
            // System's command-text reader is deliberately separate from the
            // CLI's standard input. Ordinary commands must not replace it.
            return true;
        }
        var name = current.CommandFile.Address;
        if (name.IsNull) return true;
        var length = (uint)dos.ReadUInt8(name);
        if (length == 0) return true;
        var previous = stored.ScriptTemporaryPathLength != 0
            ? stored.ScriptTemporaryPath : stored.ScriptSourcePath;
        var previousLength = stored.ScriptTemporaryPathLength != 0
            ? stored.ScriptTemporaryPathLength : stored.ScriptSourcePathLength;
        var same = length == previousLength && previous.IsNotNull;
        for (var i = 0u; same && i < length; i++)
            same = dos.ReadUInt8(previous, (int)i) == dos.ReadUInt8(name, (int)i + 1);
        // A closed source handle may be reused for the new work file. Compare
        // the CLI handoff name as well as its BPTR before retaining ownership.
        if (same && current.CurrentInput == stored.Input) return true;
        for (var i = 0u; i < length; i++)
            dos.WriteUInt8(stored.ScriptTemporaryPath, (int)i, dos.ReadUInt8(name, (int)i + 1));
        dos.WriteUInt8(stored.ScriptTemporaryPath, (int)length, 0);
        return DosShellScriptRunnerCodec.TrySetInput(ref dos, runner,
            current.CurrentInput, 1, length) &&
            ShellScriptFrameCodec.TryRead(ref this, frame, out var cursor) &&
            ShellScriptFrameCodec.TryAdvance(ref this, frame, cursor.CurrentLine + 1, 0) &&
            ShellScriptFrameCodec.TrySetInteractiveMode(ref this, frame, false);
    }

    private void FinishNativeReader(APTR cli, APTR frame)
    {
        var dos = CreateDos();
        if (!ShellScriptFrameCodec.TryRead(ref this, frame, out var stored)) return;
        var current = DosCommandLineInterfaceCodec.Read(ref dos, cli);
        DosCommandLineInterfaceCodec.WriteCurrentInput(ref dos, cli, current.StandardInput);
        var ended = (stored.Flags & (ShellScriptFrameFlags.EndRequested |
            ShellScriptFrameFlags.Interactive)) != 0;
        DosCommandLineInterfaceCodec.WriteInteractive(ref dos, cli,
            !ended && DosCore.IsInteractive(ref dos, State, current.StandardInput) != 0 ? -1 : 0);
        DosObjectCore.SetCliString(ref dos, State, cli,
            (uint)DosObjectTag.CliCommandFile, APTR.Null, 0);
    }
}
