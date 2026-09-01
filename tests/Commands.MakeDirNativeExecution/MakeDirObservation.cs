namespace CopperOS.Commands.MakeDirNativeExecution;

internal sealed class MakeDirInvocation(MakeDirCase definition, int slot, bool original)
{
    public const int InitialIoError = 31337;
    public MakeDirCase Definition { get; } = definition;
    public bool Original { get; } = original;
    public uint Process { get; } = (uint)(0x10000 + slot * 0x2000);
    public uint Arguments { get; } = (uint)(0x20000 + slot * 0x4000);
    public uint StackTop { get; } = (uint)(0x40000 + slot * 0x10000);
    public uint StackBytes => Definition.StackBytes;
    public uint DosBase { get; } = (uint)(0x8000 + slot * 0x1000);
    public uint LowestStackWrite { get; set; } = (uint)(0x40000 + slot * 0x10000);
    public int Instructions { get; set; }
    public int Opens { get; set; }
    public int Closes { get; set; }
    public int AllocationAttempts { get; set; }
    public int FreeMemCalls { get; set; }
    public int ReadArgsCalls { get; set; }
    public int FreeArgsCalls { get; set; }
    public uint ResultSlot { get; set; }
    public uint RdArgs { get; set; }
    public uint NameVector { get; set; }
    public uint[] NamePointers { get; set; } = [];
    public int DirectoryIndex { get; set; }
    public bool AwaitingCreate { get; set; }
    public uint OwnedLock { get; set; }
    public string? ExpectedDiagnostic { get; set; }
    public bool MissingNamePrinted { get; set; }
    public byte[] ProcessSnapshot { get; set; } = [];
    public byte[] ArgumentSnapshot { get; set; } = [];
    public List<byte> VPrintfBytes { get; } = [];
    public List<VPrintfObservation> VPrintfCalls { get; } = [];
    public List<FaultObservation> FaultRequests { get; } = [];
    public List<DirectoryObservation> DirectoryCalls { get; } = [];
    public List<string> SemanticEvents { get; } = [];
    public List<string> Events { get; } = [];
}

internal sealed record VPrintfObservation(string FormatHex, uint ArgumentVector,
    uint? NamePointer, string? NameHex, string RequestedBytesHex,
    string AcceptedBytesHex, int ReturnedCount);
internal sealed record FaultObservation(int Code, uint Header, int ReturnedSuccess);
internal sealed record DirectoryObservation(string Operation, string NameHex,
    uint NamePointer, int? Mode, uint RawBptr, int IoError);

internal sealed record MakeDirObservation(
    string Artifact, string CaseId, string FixtureId, bool InstructionInterleaved,
    int Result, int IoError, int ExpectedResult, int ExpectedError,
    string VPrintfBytesHex, int Instructions, uint ConfiguredStackBytes,
    uint StackBytesWritten, int OpenCalls, int CloseCalls, int ReadArgsCalls,
    int FreeArgsCalls, int ResultSlotAllocationAttempts, int ResultSlotFreeCalls,
    bool NonvolatileRegistersRestored, bool EntryStackRestored, bool GuardsAndImageUnchanged,
    IReadOnlyList<VPrintfObservation> VPrintfCalls,
    IReadOnlyList<FaultObservation> PrintFaultRequests,
    IReadOnlyList<DirectoryObservation> DirectoryCalls,
    IReadOnlyList<string> SemanticEvents, IReadOnlyList<string> Events);

internal sealed record MakeDirComparison(string CaseId, bool Passed,
    bool ResultAndErrorEqual, bool VPrintfBytesEqual, bool PrintFaultRequestsEqual,
    bool SemanticCallOrderEqual, bool RealDosParser, bool RealFilesystem);
