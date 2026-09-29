using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>
/// Resident CPU probe that enters the AddDataTypes InternalLoadSeg callback
/// exports through the SDK's A3-indirect callback wrappers.
/// </summary>
public static class NativeMorphOSAddDataTypesCallbackProbeEntry
{
    private const string ReadExport =
        "copperos.morphos.adddatatypes.load.read";
    private const string AllocateExport =
        "copperos.morphos.adddatatypes.load.alloc";
    private const string FreeExport =
        "copperos.morphos.adddatatypes.load.free";
    private const uint TestDosBase = 0x9000;
    private const uint RequestedSize = 32;

    private struct Storage
    {
        public uint CodeFirst;
        public uint CodeSecond;
        public uint CodeBuffer;
        public uint CodeSize;
        public uint CodePosition;
        public uint DestinationFirst;
        public uint DestinationSecond;

        public static APTR AddressOf(ref Storage storage) =>
            throw new System.NotSupportedException(
                "Storage.AddressOf is lowered by CopperSharp.");
    }

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var storage = default(Storage);
        var memory = Storage.AddressOf(ref storage);
        var code = memory;
        var state = APTR.FromPointer(memory.Raw + 8);
        var destination = APTR.FromPointer(memory.Raw + 20);

        APTR.WriteUInt32(code, 0, 0x11223344);
        APTR.WriteUInt32(code, 4, 0x55667788);
        APTR.WriteUInt32(state, 0, code.Raw);
        APTR.WriteUInt32(state, 4, 8);
        APTR.WriteUInt32(state, 8, 2);

        var firstRead = DosInternalSegmentCallbacks.Read(
            APTR.ExportAddress(ReadExport), BPTR.FromRaw(state.Raw),
            destination, 5, APTR.FromPointer(TestDosBase));
        var firstPosition = APTR.ReadUInt32(state, 8);
        var firstBytesMatch = APTR.ReadUInt8(destination, 0) == 0x33 &&
            APTR.ReadUInt8(destination, 1) == 0x44 &&
            APTR.ReadUInt8(destination, 2) == 0x55 &&
            APTR.ReadUInt8(destination, 3) == 0x66 &&
            APTR.ReadUInt8(destination, 4) == 0x77;

        var secondRead = DosInternalSegmentCallbacks.Read(
            APTR.ExportAddress(ReadExport), BPTR.FromRaw(state.Raw),
            destination, 4, APTR.FromPointer(TestDosBase));
        var secondPosition = APTR.ReadUInt32(state, 8);
        var endRead = DosInternalSegmentCallbacks.Read(
            APTR.ExportAddress(ReadExport), BPTR.FromRaw(state.Raw),
            destination, 4, APTR.FromPointer(TestDosBase));

        var execBase = APTR.FromPointer(
            APTR.ReadUInt32(APTR.FromPointer(4), 0));
        var allocated = DosInternalSegmentCallbacks.Allocate(
            APTR.ExportAddress(AllocateExport), RequestedSize,
            (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear), execBase);
        if (allocated.IsNotNull)
        {
            DosInternalSegmentCallbacks.Free(
                APTR.ExportAddress(FreeExport), allocated, RequestedSize,
                execBase);
        }

        return firstRead == 5 && firstPosition == 7 && firstBytesMatch &&
            secondRead == 1 && secondPosition == 8 &&
            APTR.ReadUInt8(destination, 0) == 0x88 && endRead == 0 &&
            allocated.IsNotNull ? DOS.RETURN_OK : DOS.RETURN_FAIL;
    }
}
