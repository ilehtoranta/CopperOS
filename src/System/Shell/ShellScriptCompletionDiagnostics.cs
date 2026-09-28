using Amiga;

namespace CopperOS.Shell;

/// <summary>Captures a terminal script outcome before runner cleanup changes IoErr.</summary>
public static class ShellScriptCompletionDiagnostics
{
    public static bool TryCapture<TPlatform>(ref TPlatform platform, APTR cli,
        in ShellScriptRunResult run, out ShellCommandDiagnostics diagnostics)
        where TPlatform : struct, IShellScriptPlatform
    {
        diagnostics = new ShellCommandDiagnostics { ReturnCode = run.Result };
        if (cli.IsNull || run.Status is ShellScriptStepStatus.Waiting or
            ShellScriptStepStatus.Executed or ShellScriptStepStatus.Skipped or
            ShellScriptStepStatus.Empty) return false;
        if (run.Status != ShellScriptStepStatus.EndOfFile)
            return platform.TryCaptureCommandDiagnostics(cli, run.Result, out diagnostics);

        // Reading EOF and polling signals can clear private IoErr. The public
        // CLI retains the last completed command, including an async child's
        // saved secondary result. Empty/successful scripts must not inherit
        // stale status from an earlier invocation of this CLI.
        if (run.Result == 0) return true;
        if (!platform.TryReadPublishedCommandDiagnostics(cli, out var published))
            return false;
        if (published.ReturnCode == run.Result)
            diagnostics.IoError = published.IoError;
        return true;
    }
}
