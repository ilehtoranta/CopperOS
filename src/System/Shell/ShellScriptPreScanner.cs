using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Resident preparation for Execute dot-command scripts. No source text
/// survives this call: DOS readers and caller-owned workspaces provide all
/// storage. A separate probe preserves the active reader for ordinary scripts.
/// </summary>
public enum ShellScriptPreScanResult : byte
{
    Direct = 0,
    Transformed = 1,
    Failed = 2,
}

public static class ShellScriptPreScanner
{
    public static ShellScriptPreScanResult Prepare<TPlatform>(
        ref TPlatform platform, APTR cli, APTR frame, BPTR activeInput,
        APTR sourcePath, uint sourcePathLength, APTR temporaryPath,
        uint temporaryPathCapacity, in ShellScriptStepWorkspace workspace)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
    {
        if (cli.IsNull || frame.IsNull || activeInput.IsNull ||
            sourcePath.IsNull || sourcePathLength == 0 ||
            temporaryPath.IsNull || !ValidWorkspace(ref platform, in workspace))
            return ShellScriptPreScanResult.Failed;

        if (!platform.TryOpenScriptInput(cli, sourcePath, sourcePathLength,
                out var probe))
            return ShellScriptPreScanResult.Failed;
        var probeOk = TryRead(ref platform, cli, probe, 1, 0, workspace.Line,
            workspace.LineCapacity, out var firstLength, out _, out _, out var probeEof);
        var dot = probeOk && probeEof == 0 && firstLength != 0 &&
            platform.ReadUInt8(workspace.Line, 0) == (byte)'.';
        if (!platform.TryCloseScriptRedirection(cli, probe) || !probeOk)
            return ShellScriptPreScanResult.Failed;
        if (!dot) return ShellScriptPreScanResult.Direct;

        if (!ShellScriptTemporaryPath.TryBuild(ref platform, frame,
                temporaryPath, temporaryPathCapacity, out var temporaryLength) ||
            !platform.TryOpenScriptOutput(cli, temporaryPath, temporaryLength,
                0, out var output))
            return ShellScriptPreScanResult.Failed;
        if (!platform.TryOpenScriptInput(cli, sourcePath, sourcePathLength,
                out var source))
        {
            platform.TryCloseScriptRedirection(cli, output);
            platform.TryDeleteScriptPath(cli, temporaryPath, temporaryLength);
            return ShellScriptPreScanResult.Failed;
        }

        var transformed = TryWriteTransformed(ref platform, cli, source, output,
            in workspace);
        var sourceClosed = platform.TryCloseScriptRedirection(cli, source);
        var outputClosed = platform.TryCloseScriptRedirection(cli, output);
        if (!transformed || !sourceClosed || !outputClosed ||
            !platform.TryOpenScriptInput(cli, temporaryPath, temporaryLength,
                out var replacement))
        {
            platform.TryDeleteScriptPath(cli, temporaryPath, temporaryLength);
            return ShellScriptPreScanResult.Failed;
        }
        if (!platform.TryPublishScriptInput(cli, frame, activeInput, replacement,
                temporaryPath, temporaryLength))
        {
            platform.TryCloseScriptRedirection(cli, replacement);
            platform.TryDeleteScriptPath(cli, temporaryPath, temporaryLength);
            return ShellScriptPreScanResult.Failed;
        }
        return ShellScriptPreScanResult.Transformed;
    }

    private static bool TryWriteTransformed<TPlatform>(ref TPlatform platform,
        APTR cli, BPTR source, BPTR output, in ShellScriptStepWorkspace workspace)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
    {
        var line = 1u;
        var offset = 0u;
        platform.WriteUInt8(workspace.CommandName, 0, (byte)'\n');
        while (true)
        {
            if (!TryRead(ref platform, cli, source, line, offset, workspace.Line,
                    workspace.LineCapacity, out var length, out _, out var nextOffset,
                    out var eof))
                return false;
            if (eof != 0 && length == 0) return true;

            // The temporary file is an ownership boundary, not a semantic
            // rewrite. Workbench applies directives as it reaches them, so
            // retaining every line keeps their effects source ordered.
            if (platform.Write(output, workspace.Line, length) != (int)length ||
                platform.Write(output, workspace.CommandName, 1) != 1)
                return false;
            if (eof != 0) return true;
            if (line == uint.MaxValue) return false;
            line++;
            offset = nextOffset;
        }
    }

    private static bool TryRead<TPlatform>(ref TPlatform platform, APTR cli,
        BPTR input, uint line, uint offset, APTR destination, uint capacity,
        out uint length, out uint nextLine, out uint nextOffset, out uint eof)
        where TPlatform : struct, IShellScriptPlatform
    {
        return platform.TryReadScriptLine(cli, input, line, offset, destination,
            capacity, out length, out nextLine, out nextOffset, out eof) && eof <= 1 &&
            length < capacity;
    }

    private static bool ValidWorkspace<TPlatform>(ref TPlatform platform,
        in ShellScriptStepWorkspace workspace) where TPlatform : struct, IShellPlatform =>
        workspace.Line.IsNotNull && workspace.LineCapacity >= 2 &&
        workspace.CommandName.IsNotNull && workspace.CommandNameCapacity >= 2 &&
        workspace.CommandWorkspace.Token.IsNotNull &&
        workspace.CommandWorkspace.TokenCapacity >= 2 &&
        workspace.CommandWorkspace.ErrorCodes.IsNotNull &&
        workspace.CommandWorkspace.ErrorCodeCapacity >= 4 &&
        platform.IsMapped(workspace.Line, workspace.LineCapacity) &&
        platform.IsMapped(workspace.CommandName, workspace.CommandNameCapacity) &&
        platform.IsMapped(workspace.CommandWorkspace.Token,
            workspace.CommandWorkspace.TokenCapacity) &&
        platform.IsMapped(workspace.CommandWorkspace.ErrorCodes,
            workspace.CommandWorkspace.ErrorCodeCapacity);
}
