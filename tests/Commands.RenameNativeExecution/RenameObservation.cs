namespace CopperOS.Commands.RenameNativeExecution;

internal enum RenameOperation
{
    MatchFirst, MatchNext, MatchEnd, Lock, Examine, SameLock, UnLock,
    ParsePattern, NameFromLock, Rename, IoErr, SetIoErr, VPrintf, PrintFault
}

// A fixed public-vector script, supplied independently of the original code.
// It is not a parser, wildcard engine, filesystem or replacement Rename body.
internal sealed record RenameStep(RenameOperation Operation)
{
    public string? First { get; init; }
    public string? Second { get; init; }
    public string? FullPath { get; init; }
    public string? Leaf { get; init; }
    public string? Format { get; init; }
    public string? Rendered { get; init; }
    public int Result { get; init; }
    public int IoError { get; init; } = 777;
    public int EntryType { get; init; }
    public uint RawBptr { get; init; }
    public uint OtherBptr { get; init; }
    public bool Wild { get; init; }
    public bool WriteOutput { get; init; } = true;
    public int? OutputCount { get; init; }
}

internal sealed record RenameCase(string Id, string[] Sources, string Destination,
    RenameStep[] Steps, int ExpectedResult)
{
    public uint Quiet { get; init; }
    public bool MissingDos { get; init; }
    public bool BreakPending { get; init; }
    public int FailAllocation { get; init; }
    public bool ParserFails { get; init; }
    public int ParserError { get; init; }
    public bool PoisonCleanup { get; init; } = true;
    public int ExpectedFinalIoError { get; init; } = 904;
    public string? ExpectedGuard { get; init; }
    public string? ExpectedGuardOrigin { get; init; }
    public bool SyntheticProviderCombination { get; init; }
    public uint StackBytes { get; init; } = 16384;
}

internal sealed record RenameBatch(bool Interleaved, RenameCase[] Cases);

internal sealed class RenameInvocation(RenameCase definition, int slot)
{
    public const int InitialIoError = 31337;
    public RenameCase Definition { get; } = definition;
    public uint Process { get; } = (uint)(0x10000 + slot * 0x2000);
    public uint Arguments { get; } = (uint)(0x20000 + slot * 0x4000);
    public uint StackTop { get; } = (uint)(0x40000 + slot * 0x10000);
    public uint StackBytes => Definition.StackBytes;
    public uint DosBase { get; } = (uint)(0x8000 + slot * 0x1000);
    public uint LowestStackWrite { get; set; } = (uint)(0x40000 + slot * 0x10000);
    public int Instructions { get; set; }
    public int Opens { get; set; }
    public int Closes { get; set; }
    public int CheckSignalCalls { get; set; }
    public int AllocationAttempts { get; set; }
    public uint[] VecAllocations { get; } = new uint[4];
    public int FreeVecCalls { get; set; }
    public int ReadArgsCalls { get; set; }
    public int FreeArgsCalls { get; set; }
    public uint ResultSlots { get; set; }
    public uint RdArgs { get; set; }
    public uint FromVector { get; set; }
    public uint[] FromPointers { get; set; } = [];
    public uint ToPointer { get; set; }
    public uint Anchor => VecAllocations[1];
    public uint SourceBuffer => VecAllocations[2];
    public uint DestinationBuffer => VecAllocations[3];
    public int StepIndex { get; set; }
    public int MatchFirstCalls { get; set; }
    public int MatchNextCalls { get; set; }
    public int MatchEndCalls { get; set; }
    public bool SearchLive { get; set; }
    public uint SearchToken { get; set; }
    public HashSet<uint> OwnedLocks { get; } = [];
    public uint DestinationLock { get; set; }
    public int LockCalls { get; set; }
    public int CleanupUnlockCalls { get; set; }
    public bool CleanupStarted { get; set; }
    public byte[] ProcessSnapshot { get; set; } = [];
    public byte[] ArgumentSnapshot { get; set; } = [];
    public List<byte> AcceptedVPrintfBytes { get; } = [];
    public List<RenameCall> Calls { get; } = [];
    public List<RenameOutput> Output { get; } = [];
    public List<RenameFault> Faults { get; } = [];
    public List<string> Events { get; } = [];
}

internal sealed record RenameCall(string Name, uint ProgramCounter, uint D0, uint D1,
    uint D2, uint D3, uint A1, uint A6, uint? ReturnedD0, int IoErrorBefore,
    int IoErrorAfter, bool Returned, IReadOnlyList<string> Notes);
internal sealed record RenameOutput(string FormatHex, uint ArgumentVector,
    IReadOnlyList<uint> Pointers, IReadOnlyList<string> ArgumentBytesHex,
    string RequestedBytesHex, string AcceptedBytesHex, int ReturnedCount);
internal sealed record RenameFault(int Code, uint Header, int ReturnedSuccess);
internal sealed record LiveRenameAllocation(uint Address, uint Bytes, string Kind);
internal sealed record RenameResult(string CaseId, string ContractFixture,
    bool InstructionInterleaved, bool SyntheticProviderCombination, bool Returned,
    int Result, int FinalIoError, int ExpectedResult, int ExpectedFinalIoError,
    int Instructions, uint ConfiguredStackBytes, uint StackBytesWritten,
    bool EntryStackRestored, bool NonvolatileRegistersRestored,
    bool GuardsAndImageUnchanged, int Opens, int Closes, int AllocationAttempts,
    int FreeVecCalls, int ReadArgsCalls, int FreeArgsCalls, int MatchFirstCalls,
    int MatchNextCalls, int MatchEndCalls, string AcceptedVPrintfBytesHex,
    IReadOnlyList<RenameOutput> VPrintfCalls, IReadOnlyList<RenameFault> PrintFaultRequests,
    IReadOnlyList<RenameCall> Calls);
internal sealed record RenameGuardObservation(string CaseId, string ContractFixture,
    string Guard, string Origin, string Message, int Instructions, uint ProgramCounter,
    uint StackPointer, uint RawD0AtStop, int IoErrorAtStop, int? SelectedReturnSlotAtStop,
    bool Returned, bool CommandPassed, bool GuardsAndImageUnchanged,
    int MatchFirstCalls, int MatchNextCalls, int MatchEndCalls, bool SearchLive,
    IReadOnlyList<LiveRenameAllocation> RemainingAllocations,
    IReadOnlyList<uint> RemainingLocks, IReadOnlyList<RenameOutput> VPrintfCalls,
    IReadOnlyList<RenameFault> PrintFaultRequests, IReadOnlyList<RenameCall> Calls);

internal sealed class RenameReferenceGuard(string kind, string origin, string message)
    : InvalidOperationException(message)
{
    public string Kind { get; } = kind;
    public string Origin { get; } = origin;
}
