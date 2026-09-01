using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.Exe2ArcIoNativeRoot;

/// <summary>
/// Component fixture only. The fixture supplies a live borrowed DOS base,
/// handles and scratch. Calls the exact production scanner/copy through its
/// NativeExe2ArcIo adapter; it does not open, close, parse or implement DOS.
/// </summary>
public static class Exe2ArcIoNativeProbe
{
    // Seven input LONGs: operation, raw input/output BPTRs, scratch, capacity,
    // known file/payload length, DOS base. Results: scan status/offset/length,
    // seven observation LONGs; copy status/seven observation LONGs; done.
    public const int ControlBytes = 104;

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        if (argumentLength != ControlBytes || argumentText.IsNull)
            return 20;
        APTR control = argumentText.Address;
        uint operation = APTR.ReadUInt32(control, 0);
        if (operation > 4)
            return 20;
        BPTR input = new(APTR.ReadUInt32(control, 4));
        BPTR output = new(APTR.ReadUInt32(control, 8));
        APTR scratch = new(APTR.ReadUInt32(control, 12));
        uint capacity = APTR.ReadUInt32(control, 16);
        uint knownLength = APTR.ReadUInt32(control, 20);
        // Use the public scalar-pointer intrinsic for this borrowed base.
        // The constructed-APTR setter form has a separately retained failing
        // compiler experiment; production command startup obtains its base
        // directly from Exec.OpenLibraryRaw instead of this fixture control.
        DOS.DOSLibraryBase = APTR.FromPointer(APTR.ReadUInt32(control, 24));
        NativeExe2ArcIo io = new(scratch);
        uint offset = 0;
        uint length = 0;
        Exe2ArcIoObservation scanObservation = default;
        Exe2ArcIoObservation copyObservation = default;
        Exe2ArcIoStatus scanStatus = (Exe2ArcIoStatus)uint.MaxValue;
        Exe2ArcIoStatus copyStatus = (Exe2ArcIoStatus)uint.MaxValue;
        Exe2ArcIoStatus result;

        if (operation == 2)
        {
            copyStatus = Exe2ArcPayloadCopy.Copy(ref io, input, output, scratch,
                capacity, knownLength, out copyObservation);
            result = copyStatus;
        }
        else
        {
            scanStatus = operation == 1 || operation == 4
                ? Exe2ArcForwardScanner.ScanCabinet(ref io, input, scratch,
                    capacity, knownLength, out offset, out length, out scanObservation)
                : Exe2ArcForwardScanner.ScanRar4(ref io, input, scratch,
                    capacity, knownLength, out offset, out length, out scanObservation);
            result = scanStatus;
            if (operation >= 3 && scanStatus == Exe2ArcIoStatus.Completed)
            {
                copyStatus = Exe2ArcPayloadCopy.Copy(ref io, input, output, scratch,
                    capacity, length, out copyObservation);
                result = copyStatus;
            }
        }

        APTR.WriteUInt32(control, 28, (uint)scanStatus);
        APTR.WriteUInt32(control, 32, offset);
        APTR.WriteUInt32(control, 36, length);
        StoreObservation(control, 40, ref scanObservation);
        APTR.WriteUInt32(control, 68, (uint)copyStatus);
        StoreObservation(control, 72, ref copyObservation);
        APTR.WriteUInt32(control, 100, 0x45584149); // EXAI, after production returns.
        return (int)result;
    }

    private static void StoreObservation(APTR control, int offset,
        ref Exe2ArcIoObservation observation)
    {
        APTR.WriteUInt32(control, offset, observation.HasResult ? 1u : 0u);
        APTR.WriteUInt32(control, offset + 4, (uint)observation.Stage);
        APTR.WriteUInt32(control, offset + 8, (uint)observation.RawResult);
        APTR.WriteUInt32(control, offset + 12, observation.RequestedBytes);
        APTR.WriteUInt32(control, offset + 16, observation.BytesCompleted);
        APTR.WriteUInt32(control, offset + 20, observation.IoErrCaptured ? 1u : 0u);
        APTR.WriteUInt32(control, offset + 24, (uint)observation.IoError);
    }
}
