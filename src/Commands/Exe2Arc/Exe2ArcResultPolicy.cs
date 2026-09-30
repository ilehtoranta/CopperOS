using Amiga;

namespace CopperOS.Commands;

public enum Exe2ArcCommandEvent : uint
{
    None = 0,
    ParseFailure = 1,
    UnknownType = 2,
    InputFailure = 3,
    BufferFailure = 4,
    OutputFailure = 5,
    NoMatch = 6,
    UnsafePosition = 7,
    ExtractionFailure = 8,
    Break = 9,
    Saved = 10,
}

/// <summary>
/// Command-level outcome selected by the Exe2Arc owner. It contains actions,
/// not DOS calls: the frontend still owns diagnostics, close/delete ordering
/// and final IoErr restoration. A failed extraction requests deletion of the
/// selected output after it is closed, which bounds the source's incomplete
/// output and truncation hazard.
/// </summary>
public readonly struct Exe2ArcCommandDecision
{
    internal Exe2ArcCommandDecision(int returnLevel, bool deleteOutput,
        bool reportDetection, bool reportSaved, bool reportWarning,
        Exe2ArcCommandEvent commandEvent, int ioError)
    {
        ReturnLevel = returnLevel;
        DeleteOutput = deleteOutput;
        ReportDetection = reportDetection;
        ReportSaved = reportSaved;
        ReportWarning = reportWarning;
        Event = commandEvent;
        IoError = ioError;
    }

    public int ReturnLevel { get; }
    public bool DeleteOutput { get; }
    public bool ReportDetection { get; }
    public bool ReportSaved { get; }
    public bool ReportWarning { get; }
    public Exe2ArcCommandEvent Event { get; }
    public int IoError { get; }
}

/// <summary>
/// Source-backed result precedence for the command frontend. This owner does
/// not claim original diagnostic bytes or final IoErr behavior; those remain
/// a native/reference qualification gate.
/// </summary>
public static class Exe2ArcResultPolicy
{
    public static Exe2ArcCommandDecision ParseFailure(int ioError) => Failure(
        Exe2ArcCommandEvent.ParseFailure, false, ioError);

    public static Exe2ArcCommandDecision UnknownType(int ioError = 0) => Failure(
        Exe2ArcCommandEvent.UnknownType, false, ioError);

    public static Exe2ArcCommandDecision InputFailure(int ioError) => Failure(
        Exe2ArcCommandEvent.InputFailure, false, ioError);

    public static Exe2ArcCommandDecision BufferFailure(int ioError) => Failure(
        Exe2ArcCommandEvent.BufferFailure, false, ioError);

    public static Exe2ArcCommandDecision OutputFailure(int ioError) => Failure(
        Exe2ArcCommandEvent.OutputFailure, false, ioError);

    /// <summary>
    /// Converts scanner status into the pre-output command decision. Only a
    /// completed scan is accepted; all non-match and unsafe statuses are
    /// terminal at the command boundary and cannot fall through to extraction.
    /// </summary>
    public static Exe2ArcCommandDecision ForScan(Exe2ArcIoStatus status,
        int ioError = 0)
    {
        return status switch
        {
            Exe2ArcIoStatus.Completed => new Exe2ArcCommandDecision(
                DOS.RETURN_OK, false, true, false, false,
                Exe2ArcCommandEvent.None, ioError),
            Exe2ArcIoStatus.OffsetZero or Exe2ArcIoStatus.MatchedNotPositioned =>
                Failure(Exe2ArcCommandEvent.UnsafePosition, false, ioError),
            Exe2ArcIoStatus.NoMatch => Failure(Exe2ArcCommandEvent.NoMatch,
                false, ioError),
            _ => Failure(Exe2ArcCommandEvent.InputFailure, false, ioError),
        };
    }

    /// <summary>
    /// Converts extractor status after an output has been opened. A nonzero
    /// completed byte count is the success condition; failed/empty results
    /// request close-then-delete cleanup, while the source-compatible ZIP
    /// break path reports a nonzero count and retains partial output.
    /// </summary>
    public static Exe2ArcCommandDecision ForExtraction(
        Exe2ArcIoStatus status, uint outputBytes, int ioError = 0,
        bool breakObserved = false)
    {
        if (status == Exe2ArcIoStatus.Completed && outputBytes != 0)
            return new Exe2ArcCommandDecision(DOS.RETURN_OK, false, false,
                true, true, Exe2ArcCommandEvent.Saved, ioError);

        var commandEvent = status == Exe2ArcIoStatus.IoStopped &&
            ioError == (int)DOS.Error.Break
            ? Exe2ArcCommandEvent.Break
            : Exe2ArcCommandEvent.ExtractionFailure;
        return Failure(commandEvent, true, ioError);
    }

    private static Exe2ArcCommandDecision Failure(
        Exe2ArcCommandEvent commandEvent, bool deleteOutput, int ioError) =>
        new(DOS.RETURN_FAIL, deleteOutput, false, false, false,
            commandEvent, ioError);
}
