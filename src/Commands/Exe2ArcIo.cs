using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Borrowed DOS I/O and guest scratch used by the Exe2Arc components. Methods
/// return the raw DOS LONG without retries or error capture. The component
/// calls IoErr immediately and only after an observed -1 result.
/// </summary>
public interface IExe2ArcIo : IAmigaGuestMemory
{
    int Read(BPTR file, APTR buffer, int length);
    int Write(BPTR file, APTR buffer, int length);
    int Seek(BPTR file, int position, int mode);
    int IoErr();
}

/// <summary>
/// Optional command-level signal owner. The original Exe2Arc only polls
/// Ctrl-C in the ZIP record loop; when observed it sets ERROR_BREAK without
/// forcing a failing command return. Component callers that do not model DOS
/// signals simply omit this interface and retain the deterministic no-signal
/// path.
/// </summary>
public interface IExe2ArcBreakSource
{
    bool IsBreakPending();
    void SetIoErr(int ioError);
}

/// <summary>Component results, not AmigaDOS command return levels.</summary>
public enum Exe2ArcIoStatus : uint
{
    Completed = 0,
    NoMatch = 1,
    OffsetZero = 2,
    IoStopped = 3,
    MatchedNotPositioned = 4,
    InvalidBuffer = 5,
    UnsupportedRange = 6,
    InvalidHandle = 7,
    InvalidRecord = 8,
}

public enum Exe2ArcIoStage : uint
{
    None = 0,
    WindowSeek = 1,
    WindowRead = 2,
    CandidateSeek = 3,
    PayloadSeek = 4,
    PayloadRead = 5,
    PayloadWrite = 6,
    DispatchSeek = 7,
}

/// <summary>
/// Last I/O observation. Default means no call was made; its zero fields do
/// not claim that the process's IoErr was zero. This value owns no resource.
/// </summary>
public readonly struct Exe2ArcIoObservation
{
    private readonly uint _hasResult;
    private readonly uint _errorCaptured;
    private readonly Exe2ArcIoStage _stage;
    private readonly int _rawResult;
    private readonly int _ioError;
    private readonly uint _requestedBytes;
    private readonly uint _bytesCompleted;
    private readonly uint _breakObserved;
    private readonly int _breakError;

    internal Exe2ArcIoObservation(Exe2ArcIoStage stage, int rawResult,
        uint requestedBytes, uint bytesCompleted, bool errorCaptured, int ioError)
    {
        _hasResult = 1;
        _errorCaptured = errorCaptured ? 1u : 0u;
        _stage = stage;
        _rawResult = rawResult;
        _ioError = ioError;
        _requestedBytes = requestedBytes;
        _bytesCompleted = bytesCompleted;
        _breakObserved = 0;
        _breakError = 0;
    }

    private Exe2ArcIoObservation(Exe2ArcIoObservation source, int breakError)
    {
        _hasResult = source._hasResult;
        _errorCaptured = source._errorCaptured;
        _stage = source._stage;
        _rawResult = source._rawResult;
        _ioError = source._ioError;
        _requestedBytes = source._requestedBytes;
        _bytesCompleted = source._bytesCompleted;
        _breakObserved = 1;
        _breakError = breakError;
    }

    public bool HasResult => _hasResult != 0;
    public Exe2ArcIoStage Stage => _stage;
    public int RawResult => _rawResult;
    public bool IoErrCaptured => _errorCaptured != 0;
    public int IoError => _ioError;

    /// <summary>Read/Write request length; zero for Seek.</summary>
    public uint RequestedBytes => _requestedBytes;

    /// <summary>
    /// Confirmed output bytes. A positive short Write is included when its
    /// count does not exceed the request. A -1 may have unreported side effects;
    /// this count does not assert that they did not occur. Scanning writes no
    /// output and therefore always reports zero here.
    /// </summary>
    public uint BytesCompleted => _bytesCompleted;

    /// <summary>True when the optional command signal owner observed Ctrl-C.</summary>
    public bool BreakObserved => _breakObserved != 0;

    /// <summary>
    /// Error assigned by the signal owner when BreakObserved is true. This is
    /// separate from IoErrCaptured because no failing DOS transfer occurred.
    /// </summary>
    public int BreakError => _breakError;

    internal Exe2ArcIoObservation WithBreak(int ioError) =>
        new(this, ioError);
}

internal static class Exe2ArcIoBounds
{
    // Fixed source command scan/copy size, not an arbitrary caller stride.
    internal const uint BufferBytes = 102400;

    internal static bool HasScratch<TIo>(ref TIo io, APTR scratch, uint capacity)
        where TIo : struct, IExe2ArcIo =>
        capacity >= BufferBytes && !scratch.IsNull &&
        scratch.Raw <= uint.MaxValue - (BufferBytes - 1) &&
        io.IsMapped(scratch, BufferBytes);

    internal static Exe2ArcIoObservation Observe<TIo>(ref TIo io,
        Exe2ArcIoStage stage, int rawResult, uint requested, uint completed)
        where TIo : struct, IExe2ArcIo
    {
        bool captured = rawResult == -1;
        int error = captured ? io.IoErr() : 0;
        return new Exe2ArcIoObservation(stage, rawResult, requested, completed,
            captured, error);
    }
}
