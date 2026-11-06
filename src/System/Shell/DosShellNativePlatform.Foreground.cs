using Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

public partial struct DosShellNativePlatform
{
    private bool ExecuteForeground(APTR cli, APTR frame,
        in ShellScriptCommandInvocation command, in ShellScriptLookupResult lookup,
        BPTR input, BPTR output, BPTR error, out int result, out APTR continuation)
    {
        var dos = new CopperSharpNativeDosPlatform();
        continuation = APTR.Null;
        var executed = lookup.Kind == ShellScriptLookupKind.Script
            ? DosShellNativeForegroundCore.ExecuteScript(dos, cli,
                in command, in lookup, input, output, error, out result)
            : DosShellNativeForegroundCore.Execute(dos, cli,
                in command, in lookup, input, output, error, out result);
        if (!TryGetShellError(out var savedError)) return false;
        // Loader/I/O/allocation errors are command failures. The interactive
        // Shell keeps reading after publishing their DOS error.
        if (!executed) result = (int)ShellCommandResult.Error;
        var adopted = !executed || AdoptNativeCommandInput(cli, frame);
        _ = SetShellError((DOS.Error)savedError);
        return adopted;
    }

    private bool AdoptNativeCommandInput(APTR cli, APTR frame)
    {
        var dos = CreateDos();
        var runner = FindRunner(cli);
        if (runner.IsNull || !ShellScriptRunnerCodec.TryRead(ref dos, runner, out var stored) || stored.Frame != frame) return false;
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
        if (!ShellScriptRunnerCodec.TrySetInput(ref dos, runner,
            current.CurrentInput, 1, length)) return false;
        if (stored.InputOwned == 2 && !ShellCommandSource.Release(ref this, stored.Input)) return false;
        return
            ShellScriptFrameCodec.TryRead(ref this, frame, out var cursor) &&
            ShellScriptFrameCodec.TryAdvance(ref this, frame, cursor.CurrentLine + 1, 0) &&
            ShellScriptFrameCodec.TrySetInteractiveMode(ref this, frame, false);
    }

    private void FinishNativeReader(APTR cli, APTR frame)
    {
        var dos = CreateDos();
        if (!ShellScriptFrameCodec.TryRead(ref this, frame, out var stored)) return;
        var current = DosCommandLineInterfaceCodec.Read(ref dos, cli);
        var runner = FindRunner(cli);
        var borrowedInput = runner.IsNotNull &&
            ShellScriptRunnerCodec.TryRead(ref dos, runner, out var reader) &&
            reader.InputOwned == 0 && reader.Input == current.CurrentInput &&
            current.CurrentInput != current.StandardInput;
        // Packet startup owns this borrowed stream. Leave it visible for its
        // owner to close; Execute can instead close it and adopt a new reader.
        if (!borrowedInput)
            DosCommandLineInterfaceCodec.WriteCurrentInput(ref dos, cli, current.StandardInput);
        var ended = (stored.Flags & (ShellScriptFrameFlags.EndRequested |
            ShellScriptFrameFlags.Interactive)) != 0;
        if ((stored.Flags & ShellScriptFrameFlags.EndRequested) != 0)
            _ = ShellProcessState.TryRequestEnd(ref this, cli);
        DosCommandLineInterfaceCodec.WriteInteractive(ref dos, cli,
            !ended && FileIsInteractive(current.StandardInput) != 0 ? -1 : 0);
        _ = ShellCliState.TryClearCommandFile(ref dos, cli);
    }
}
