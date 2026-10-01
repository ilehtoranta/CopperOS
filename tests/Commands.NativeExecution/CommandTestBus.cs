using System.Buffers.Binary;
using System.Text;
using Copper68k;

namespace CopperOS.Commands.NativeExecution;

internal sealed class CommandTestBus : IM68kBus
{
    private readonly Dictionary<uint, Action<M68kCpuState>> gateways = new();
    private readonly Dictionary<uint, Allocation> allocations = new();
    private readonly List<(uint Start, uint Size)> nativeReadableRegions = [];
    private uint nextAllocation = 0x60000;
    private readonly List<(uint Start, uint Length, byte[] Snapshot)> protectedImages = [];
    private readonly List<(uint Start, uint Size)> writableImageRegions = [];

    // Join owns a 262144-byte transfer buffer. Keep the fixture arena large
    // enough for repeated, released invocations without changing its guard
    // and ownership rules.
    public byte[] Memory { get; } = new byte[0x1000000];
    public Invocation? Current { get; set; }
    public int NativeWrites { get; private set; }
    public int NativeReads { get; private set; }

    public void LoadAndProtect(uint address, byte[] code)
    {
        LoadImage(address, code, [(0, code.Length)], []);
    }

    public void LoadImage(uint address, byte[] image,
        IEnumerable<(int Offset, int Length)> readOnlyRanges,
        IEnumerable<(int Offset, int Length)> writableRanges)
    {
        image.CopyTo(Memory.AsSpan(Offset(address, image.Length)));
        foreach (var (rangeOffset, rangeLength) in readOnlyRanges)
        {
            ValidateImageRange(rangeOffset, rangeLength, image.Length);
            var start = checked(address + (uint)rangeOffset);
            var snapshot = image.AsSpan(rangeOffset, rangeLength).ToArray();
            protectedImages.Add((start, (uint)rangeLength, snapshot));
        }
        foreach (var (rangeOffset, rangeLength) in writableRanges)
        {
            ValidateImageRange(rangeOffset, rangeLength, image.Length);
            writableImageRegions.Add((checked(address + (uint)rangeOffset), (uint)rangeLength));
        }
    }

    public void AssertImageUnchanged()
    {
        foreach (var (start, length, snapshot) in protectedImages)
            Require(Memory.AsSpan((int)start, (int)length).SequenceEqual(snapshot),
                "Shared code/constants changed.");
    }

    private static void ValidateImageRange(int offset, int length, int imageLength)
    {
        if (offset < 0 || length <= 0 || offset > imageLength - length)
            throw new InvalidDataException("HUNK image range is invalid.");
    }

    public void RegisterGateway(uint address, Action<M68kCpuState> handler)
    {
        gateways.Add(address, handler);
        Word(address, 0xff00);
        Long(address + 2, 1);
    }

    public bool HasHostGateway(uint address) => gateways.ContainsKey(address);

    public bool TryInvokeHostGateway(uint instructionProgramCounter, uint token, M68kCpuState state)
    {
        if (!gateways.TryGetValue(instructionProgramCounter, out var handler)) return false;
        Require(token == 1, "Unexpected host gateway token.");
        handler(state);
        return true;
    }

    public byte ReadByte(uint address, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckNativeRead(address, 1, accessKind);
        return Memory[Offset(address, 1)];
    }
    public ushort ReadWord(uint address, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckNativeRead(address, 2, accessKind);
        return Word(address);
    }
    public uint ReadLong(uint address, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckNativeRead(address, 4, accessKind);
        return Long(address);
    }
    public void WriteByte(uint address, byte value, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckNativeWrite(address, 1);
        Memory[Offset(address, 1)] = value;
    }
    public void WriteWord(uint address, ushort value, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckNativeWrite(address, 2);
        Word(address, value);
    }
    public void WriteLong(uint address, uint value, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckNativeWrite(address, 4);
        Long(address, value);
    }
    public void ResetExternalDevices(long cycle) => throw new InvalidOperationException("Command executed RESET.");

    public ushort Word(uint address) => BinaryPrimitives.ReadUInt16BigEndian(Memory.AsSpan(Offset(address, 2), 2));
    public void Word(uint address, ushort value) => BinaryPrimitives.WriteUInt16BigEndian(Memory.AsSpan(Offset(address, 2), 2), value);
    public uint Long(uint address) => BinaryPrimitives.ReadUInt32BigEndian(Memory.AsSpan(Offset(address, 4), 4));
    public void Long(uint address, uint value) => BinaryPrimitives.WriteUInt32BigEndian(Memory.AsSpan(Offset(address, 4), 4), value);
    public string CString(uint address)
    {
        var start = Offset(address, 1);
        var end = start;
        while (end < Memory.Length && Memory[end] != 0 && end - start < 4096) end++;
        Require(end < Memory.Length && Memory[end] == 0,
            $"Unterminated C string at ${address:X8}.");
        return Encoding.Latin1.GetString(Memory, start, end - start);
    }

    public uint Allocate(Invocation owner, uint size, string kind, bool clear)
    {
        Require(size > 0 && size < 0x1000000, "Unexpected fixture allocation size.");
        var address = checked((nextAllocation + 23) & ~7u);
        foreach (var (start, length) in protectedImages.Select(region => (region.Start, region.Length))
                     .Concat(writableImageRegions))
        {
            var allocationEnd = (ulong)address + size + 32u;
            if (address < (ulong)start + length && allocationEnd > start)
                address = checked((start + length + 0x1000u + 7u) & ~7u);
        }
        nextAllocation = checked(address + size + 16);
        Require(nextAllocation < 0xf00000, "Fixture allocation arena exhausted.");
        Memory.AsSpan((int)address - 16, checked((int)size + 32)).Fill(0xa7);
        Memory.AsSpan((int)address, (int)size).Fill(clear ? (byte)0 : (byte)0xcd);
        allocations.Add(address, new Allocation(owner, address, size, kind));
        return address;
    }

    public Allocation OwnedAllocation(Invocation owner, uint address, string kind)
    {
        Require(allocations.TryGetValue(address, out var allocation) &&
            ReferenceEquals(allocation.Owner, owner) && allocation.Kind == kind,
            $"Invalid {kind} ownership at ${address:X8}.");
        return allocation!;
    }

    public void RegisterNativeReadableRegion(uint address, uint size)
    {
        Require(size != 0 && (ulong)address + size <= (ulong)Memory.Length,
            "Invalid fixture-readable memory region.");
        if (!nativeReadableRegions.Contains((address, size)))
            nativeReadableRegions.Add((address, size));
    }

    public Allocation OwnedAllocationContaining(Invocation owner, uint address,
        string kind)
    {
        var allocation = allocations.Values.SingleOrDefault(candidate =>
            ReferenceEquals(candidate.Owner, owner) && candidate.Kind == kind &&
            address >= candidate.Address &&
            (ulong)address < (ulong)candidate.Address + candidate.Size);
        Require(allocation is not null,
            $"Invalid {kind} ownership at ${address:X8}.");
        return allocation!;
    }

    public void Release(Invocation owner, uint address, string kind, uint? size = null)
    {
        var allocation = OwnedAllocation(owner, address, kind);
        Require(size is null || size == allocation.Size, "FreeMem size differs from AllocMem.");
        Require(Memory.AsSpan((int)address - 16, 16).IndexOfAnyExcept((byte)0xa7) < 0 &&
            Memory.AsSpan((int)(address + allocation.Size), 16).IndexOfAnyExcept((byte)0xa7) < 0,
            "Allocation guard changed.");
        allocations.Remove(address);
        Memory.AsSpan((int)address, (int)allocation.Size).Fill(0xdd);
    }

    public void AssertReleased(Invocation owner) => Require(!allocations.Values.Any(a => ReferenceEquals(a.Owner, owner)),
        "Invocation leaked guest memory or RDArgs: " + string.Join(",", allocations.Values
            .Where(a => ReferenceEquals(a.Owner, owner))
            .Select(a => $"{a.Kind}@${a.Address:X8}+{a.Size}")));

    private void CheckNativeWrite(uint address, int size)
    {
        _ = Offset(address, size);
        Require(!protectedImages.Any(region => address < (ulong)region.Start + region.Length &&
            (ulong)address + (uint)size > region.Start),
            $"Native write to shared image at ${address:X8}.");
        var owner = Current ?? throw new InvalidOperationException("No active process for native write.");
        if (owner.IconPosLayout is { } iconLayout &&
            ((iconLayout.Icon != 0 && address >= iconLayout.Icon && address < iconLayout.Icon + 128) ||
             (iconLayout.DefaultObject != 0 && address >= iconLayout.DefaultObject && address < iconLayout.DefaultObject + 128) ||
             (iconLayout.DrawerData != 0 && address >= iconLayout.DrawerData && address < iconLayout.DrawerData + 16)))
            owner.IconWriteAddresses.Add(address);
        var stackWrite = address >= owner.StackTop - owner.StackBytes && (ulong)address + (uint)size <= owner.StackTop;
        var ioOutputWrite = owner.NativeIo?.ContainsOutput(address, size) == true;
        var copyFilePairStorage = owner.CopyFilePairLayout?.Contains(address, size) == true;
        var copyDestinationStorage = owner.CopyDestinationLayout?.Contains(address, size) == true;
        var copyDestinationDirectoriesStorage = owner.CopyDestinationDirectoriesLayout?.Contains(address, size) == true;
        var copyNonFileSystemStorage = owner.CopyNonFileSystemLayout?.Contains(address, size) == true;
        var copyLoopGuardStorage = owner.CopyLoopGuardLayout?.Contains(address, size) == true;
        var copyMetadataStorage = owner.CopyMetadataLayout?.Contains(address, size) == true || (owner.Definition.CopyArgumentGate?.SetupValues is not null && address >= owner.Arguments && (ulong)address + (uint)size <= (ulong)owner.Arguments + 32);
        var copyResultPolicyStorage = owner.CopyResultPolicyLayout?.Contains(address, size) == true;
        var copyOpenDestinationStorage = owner.CopyOpenDestinationLayout?.Contains(address, size) == true;
        var copyPatternClassifierStorage = owner.CopyPatternClassifierLayout?.Contains(address, size) == true;
        var copyFlatTraversalStorage = owner.CopyFlatTraversalLayout?.Contains(address, size) == true;
        var copySoftLinkStorage = owner.CopySoftLinkLayout?.Contains(address, size) == true;
        var copyMatchStepStorage = owner.CopyMatchStepLayout?.Contains(address, size) == true;
        var copyTraversalStorage = owner.CopyTraversalLayout?.Contains(address, size) == true;
        var copyWorkPreparationStorage = owner.CopyWorkPreparationLayout?.Contains(address, size) == true;
        var copyFileTransferStorage = owner.CopyFileTransferLayout?.Contains(address, size) == true;
        var copyFileOperationStorage = owner.CopyFileOperationLayout?.Contains(address, size) == true;
        var copyLinkOperationStorage = owner.CopyLinkOperationLayout?.Contains(address, size) == true;
        var copyDirectoryOperationStorage = owner.CopyDirectoryOperationLayout?.Contains(address, size) == true;
        var copyOutputStorage = owner.CopyOutputLayout?.Contains(address, size) == true;
        var copyWorkStorage = owner.CopyWorkLayout?.Contains(address, size) == true;
        var copyTraversalWorkStorage = owner.CopyTraversalWorkLayout?.Contains(address, size) == true;
        var copyDirectoryEntryStorage = owner.CopyDirectoryEntryLayout?.Contains(address, size) == true;
        var copyDirectoryExitStorage = owner.CopyDirectoryExitLayout?.Contains(address, size) == true;
        var copyRequesterWrite = (owner.CopyArgumentGateLayout is not null || owner.CopyTraversalWorkLayout is not null) && address >= owner.Process + (uint)Amiga.DosLayout.Process.WindowPointer && (ulong)address + (uint)size <= owner.Process + (uint)Amiga.DosLayout.Process.WindowPointer + 4;
        // Workbench Info disables DOS requesters (pr_WindowPtr = -1) while it probes devices.
        var infoWindowPointerWrite = owner.Definition.Info is not null &&
            address >= owner.Process + (uint)Amiga.DosLayout.Process.WindowPointer &&
            (ulong)address + (uint)size <= owner.Process + (uint)Amiga.DosLayout.Process.WindowPointer + 4;
        var executeWindowPointerWrite = owner.AllowsWindowPointerWrite &&
            address >= owner.Process + (uint)Amiga.DosLayout.Process.WindowPointer &&
            (ulong)address + (uint)size <= owner.Process + (uint)Amiga.DosLayout.Process.WindowPointer + 4;
        var guessBootDevProcessWrite = owner.Definition.GuessBootDev is not null &&
            address >= owner.Process + (uint)Amiga.DosLayout.Process.WindowPointer &&
            (ulong)address + (uint)size <= owner.Process + (uint)Amiga.DosLayout.Process.WindowPointer + 4;
        var addDataTypesWindowPointerWrite =
            owner.Definition.AddDataTypesList?.Refresh == true &&
            address >= owner.Process +
                (uint)Amiga.DosLayout.Process.WindowPointer &&
            (ulong)address + (uint)size <= owner.Process +
                (uint)Amiga.DosLayout.Process.WindowPointer + 4;
        var versionAmbientWindowPointerWrite = owner.Definition.Version is
                { System: true } &&
            address >= owner.Process +
                (uint)Amiga.DosLayout.Process.WindowPointer &&
            (ulong)address + (uint)size <= owner.Process +
                (uint)Amiga.DosLayout.Process.WindowPointer + 4;
        var addDataTypesSharedStorage =
            owner.Definition.AddDataTypesList?.ContainsWrite(address, size) == true;
        var ownProcessErrorWrite = owner.Definition.WritesOwnProcessError && owner.Definition.MissingDos &&
            address >= owner.Process + (uint)Amiga.DosLayout.Process.Result2 &&
            (ulong)address + (uint)size <= owner.Process + (uint)Amiga.DosLayout.Process.Result2 + 4;
        var writableImage = writableImageRegions.Any(region => address >= region.Start &&
            (ulong)address + (uint)size <= (ulong)region.Start + region.Size);
        Require(writableImage || ownProcessErrorWrite || copyRequesterWrite || executeWindowPointerWrite || guessBootDevProcessWrite || infoWindowPointerWrite || addDataTypesWindowPointerWrite || versionAmbientWindowPointerWrite || addDataTypesSharedStorage || stackWrite || ioOutputWrite || copyFilePairStorage || copyDestinationStorage || copyDestinationDirectoriesStorage || copyNonFileSystemStorage || copyLoopGuardStorage || copyMetadataStorage || copyResultPolicyStorage || copyOpenDestinationStorage || copyPatternClassifierStorage || copyFlatTraversalStorage || copyDirectoryExitStorage || copyDirectoryEntryStorage || copyTraversalWorkStorage || copyWorkStorage || copyOutputStorage || copyDirectoryOperationStorage || copyLinkOperationStorage || copyFileOperationStorage || copyFileTransferStorage || copyWorkPreparationStorage || copyTraversalStorage || copyMatchStepStorage || copySoftLinkStorage || owner.NativeIo is null && allocations.Values.Any(a => ReferenceEquals(a.Owner, owner) &&
            address >= a.Address && (ulong)address + (uint)size <= (ulong)a.Address + a.Size),
            $"Native write outside invocation-owned storage at ${address:X8}.");
        if (stackWrite) owner.LowestStackWrite = Math.Min(owner.LowestStackWrite, address);
        if (ioOutputWrite) owner.NativeIo!.RecordOutputWrite(address, size);
        NativeWrites++;
    }

    private void CheckNativeRead(uint address, int size, M68kBusAccessKind accessKind)
    {
        _ = Offset(address, size);
        if (Current is { NativeIo: { } io } ioOwner)
        {
            var ownStack = address >= ioOwner.StackTop - ioOwner.StackBytes &&
                (ulong)address + (uint)size <= ioOwner.StackTop;
            var ownProcess = address >= ioOwner.Process &&
                (ulong)address + (uint)size <= (ulong)ioOwner.Process + 0x400;
            var imageRead = protectedImages.Any(region => address >= region.Start &&
                (ulong)address + (uint)size <= (ulong)region.Start + region.Length) ||
                writableImageRegions.Any(region => address >= region.Start &&
                    (ulong)address + (uint)size <= (ulong)region.Start + region.Size);
            var execPointerRead = address >= 4 && (ulong)address + (uint)size <= 8;
            var instructionFetch = accessKind == M68kBusAccessKind.CpuInstructionFetch;
            // The six-byte host-call stub may prefetch two following words.
            // RTS also prefetches its caller before the loop sees the sentinel.
            // These extra bytes are fetch-only and never writable storage.
            var gatewayRead = gateways.Keys.Any(start => address >= start &&
                (ulong)address + (uint)size <= (ulong)start + (instructionFetch ? 10u : 6u));
            var callerPrefetch = instructionFetch && address >= 0x2000 &&
                (ulong)address + (uint)size <= 0x2008;
            Require(ownStack || ownProcess || imageRead || execPointerRead || gatewayRead || callerPrefetch ||
                io.ContainsReadableControl(address, size) || io.ContainsReadablePayload(address, size),
                $"Native I/O read outside the current task's readable storage at ${address:X8}.");
        }
        var inProtectedImage = protectedImages.Any(region => address >= region.Start &&
                (ulong)address + (uint)size <= (ulong)region.Start + region.Length) ||
            writableImageRegions.Any(region => address >= region.Start &&
                (ulong)address + (uint)size <= (ulong)region.Start + region.Size);
        var fixtureReadable = nativeReadableRegions.Any(region =>
            address >= region.Start && (ulong)address + (uint)size <=
                (ulong)region.Start + region.Size);
        if (!fixtureReadable && !inProtectedImage && address < nextAllocation && (ulong)address +
            (uint)size > 0x60000)
        {
            var owner = Current ?? throw new InvalidOperationException("No active process for native allocation read.");
            Require(allocations.Values.Any(a => ReferenceEquals(a.Owner, owner) &&
                address >= a.Address && (ulong)address + (uint)size <= (ulong)a.Address + a.Size),
                $"Native read of freed, guarded, or another invocation's allocation at ${address:X8}; " +
                $"owned=[{string.Join(",", allocations.Values.Where(a => ReferenceEquals(a.Owner, owner)).Select(a => $"{a.Kind}:${a.Address:X8}+{a.Size}"))}]; " +
                $"joinBuffers=[{string.Join(",", owner.JoinLayout?.Buffers.Select(a => $"${a:X8}") ?? [])}], " +
                $"joinSource={owner.JoinLayout?.CurrentSourceIndex ?? -1}, " +
                $"joinReads={JoinIndices(owner.JoinLayout?.SourceReadIndices)}, " +
                $"joinWrites={JoinIndices(owner.JoinLayout?.SourceWriteIndices)}.");
        }
        NativeReads++;
    }

    private int Offset(uint address, int size)
    {
        if (size < 0 || address > Memory.Length || size > Memory.Length - (long)address)
            throw new InvalidOperationException($"Unmapped bus address ${address:X8} size {size}.");
        return (int)address;
    }

    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal sealed record Allocation(Invocation Owner, uint Address, uint Size, string Kind);

    private static string JoinIndices(int[]? values) => values is null ? "" :
        string.Join(",", values);
}
